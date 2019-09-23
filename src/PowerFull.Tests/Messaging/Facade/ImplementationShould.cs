using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mqtt;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace PowerFull.Tests.Messaging.Facade
{
    [TestFixture]
    public class ImplementationShould
    {
        private static readonly PowerFull.Device.Implementation DeviceA = new PowerFull.Device.Implementation
        {
            Id = "DeviceA",
            PowerStateRequestTopic = "PowerStateRequestTopicA",
            PowerStateRequestPayload = "Whats the frequency Kenneth?",
            PowerStateResponseTopic = "PowerStateResponseTopicA",
            PowerOnRequestTopic = "PowerOnRequestTopicA",
            PowerOffRequestTopic = "PowerOffRequestTopicA"
        };

        private static readonly PowerFull.Device.Implementation DeviceB = new PowerFull.Device.Implementation
        {
            Id = "DeviceB",
            PowerStateRequestTopic = "PowerStateRequestTopicB",
            PowerStateResponseTopic = "PowerStateResponseTopicB",
            PowerOnRequestTopic = "PowerOnRequestTopicB",
            PowerOffRequestTopic = "PowerOffRequestTopicB"
        };

        private static async Task<(Subject<MqttApplicationMessage>, IMqttClient, PowerFull.Messaging.Facade.Implementation)> CreateSubject(PowerFull.Messaging.Config config = null, IEnumerable<IDevice> devices = null)
        {
            config = config ?? new PowerFull.Messaging.Config();
            var messages = new Subject<MqttApplicationMessage>();
            var mqttClient = A.Fake<IMqttClient>();
            A.CallTo(() => mqttClient.MessageStream).Returns(messages);
            var mqttFactory = A.Fake<PowerFull.Messaging.Mqtt.IFactory>();
            A.CallTo(() => mqttFactory.Create()).Returns(new ValueTask<IMqttClient>(mqttClient));
            var logger = A.Fake<ILogger<PowerFull.Messaging.IFacade>>();

            var subject = new PowerFull.Messaging.Facade.Implementation(config, mqttFactory, devices, logger);
            await subject.InitializeAsync();

            return (messages, mqttClient, subject);
        }

        [Test]
        public async Task SubscribeToExpectedTopics()
        {
            var config = new PowerFull.Messaging.Config
            {
                PowerReadingTopic = "TestPowerReadingTopic"
            };

            (var messages, var mqttClient, var subject) = await CreateSubject(config: config, devices: new[] { DeviceA, DeviceB });

            A.CallTo(() => mqttClient.SubscribeAsync("PowerStateRequestTopicA", MqttQualityOfService.AtLeastOnce)).MustHaveHappenedOnceExactly();
            A.CallTo(() => mqttClient.SubscribeAsync("PowerStateResponseTopicA", MqttQualityOfService.AtLeastOnce)).MustHaveHappenedOnceExactly();
            A.CallTo(() => mqttClient.SubscribeAsync("PowerOnRequestTopicA", MqttQualityOfService.AtLeastOnce)).MustHaveHappenedOnceExactly();
            A.CallTo(() => mqttClient.SubscribeAsync("PowerOffRequestTopicA", MqttQualityOfService.AtLeastOnce)).MustHaveHappenedOnceExactly();
            A.CallTo(() => mqttClient.SubscribeAsync("PowerStateRequestTopicB", MqttQualityOfService.AtLeastOnce)).MustHaveHappenedOnceExactly();
            A.CallTo(() => mqttClient.SubscribeAsync("PowerStateResponseTopicB", MqttQualityOfService.AtLeastOnce)).MustHaveHappenedOnceExactly();
            A.CallTo(() => mqttClient.SubscribeAsync("PowerOnRequestTopicB", MqttQualityOfService.AtLeastOnce)).MustHaveHappenedOnceExactly();
            A.CallTo(() => mqttClient.SubscribeAsync("PowerOffRequestTopicB", MqttQualityOfService.AtLeastOnce)).MustHaveHappenedOnceExactly();
            A.CallTo(() => mqttClient.SubscribeAsync("TestPowerReadingTopic", MqttQualityOfService.AtLeastOnce)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task PublishAMessageOnThePowerStateRequestTopicWhenPowerStateRequested()
        {
            (var messages, var mqttClient, var subject) = await CreateSubject(devices: new[] { DeviceA, DeviceB });

            _ = subject.GetPowerState(DeviceA);

            A.CallTo(
                () => mqttClient.PublishAsync(
                    A<MqttApplicationMessage>.That.Matches(message => message.Topic.Equals("PowerStateRequestTopicA")),
                    A<MqttQualityOfService>.Ignored,
                    false))
             .MustHaveHappenedOnceExactly();
        }


        [Test]
        public async Task PublishThePowerStateRequestPayloadMessageWhenPowerStateRequested()
        {
            (var messages, var mqttClient, var subject) = await CreateSubject(devices: new[] { DeviceA, DeviceB });
            await subject.InitializeAsync();

            _ = subject.GetPowerState(DeviceA);

            A.CallTo(
                () => mqttClient.PublishAsync(
                    A<MqttApplicationMessage>.That.Matches(message => Encoding.UTF8.GetString(message.Payload).Equals("Whats the frequency Kenneth?")),
                    A<MqttQualityOfService>.Ignored,
                    false))
             .MustHaveHappenedOnceExactly();
        }

        private static IEnumerable<TestCaseData> PowerStates
        {
            get
            {
                yield return new TestCaseData("ON", "OFF", "ON")
                    .Returns(PowerState.On)
                    .SetName("Return On For Simple Regex Match");

                yield return new TestCaseData("ON", "OFF", "OFF")
                    .Returns(PowerState.Off)
                    .SetName("Return Off For Simple Regex Match");

                yield return new TestCaseData("ON", "OFF", "{ \"POWER\":\"ON\" }")
                    .Returns(PowerState.On)
                    .SetName("Return On For Simple Json Regex Match");

                yield return new TestCaseData("ON", "OFF", "{ \"POWER\":\"OFF\" }")
                    .Returns(PowerState.Off)
                    .SetName("Return Off For Simple Json Regex Match");

                yield return new TestCaseData("^\\{\"POWER\":\"ON\"\\}$", "^\\{\"POWER\":\"OFF\"\\}$", "{\"POWER\":\"ON\"}")
                    .Returns(PowerState.On)
                    .SetName("Return On For Advanced Json Regex Match");

                yield return new TestCaseData("^\\{\"POWER\":\"ON\"\\}$", "^\\{\"POWER\":\"OFF\"\\}$", "{\"POWER\":\"OFF\"}")
                    .Returns(PowerState.Off)
                    .SetName("Return Off For Advanced Json Regex Match");

            }
        }

        [Test, TestCaseSource(nameof(PowerStates))]
        public async Task<PowerState> ReturnThePowerStateWhenAResponseIsReceived(
            string powerOnPayloadRegex, 
            string powerOffPayloadRegex, 
            string payload)
        {
            const string topic = "powerStateResponse";

            var device = new PowerFull.Device.Implementation
            {
                Id = "device",
                PowerStateResponseTopic = topic,
                PowerStateResponseOnPayloadRegex = powerOnPayloadRegex,
                PowerStateResponseOffPayloadRegex = powerOffPayloadRegex
            };

            (var messages, var mqttClient, var subject) = await CreateSubject(devices: new[] { device });

            var powerState = subject.GetPowerState(device);

            messages.OnNext(new MqttApplicationMessage(topic, Encoding.UTF8.GetBytes(payload)));

            var actual = await powerState;

            return actual;
        }

        private static IEnumerable<TestCaseData> PowerReadings
        {
            get
            {
                yield return new TestCaseData(
                    @"^(?<RealPower>\d+(\.\d+)?)$",
                    new[]
                    {
                        Recorded.OnNext(TimeSpan.FromSeconds(1), "2100"),
                        Recorded.OnNext(TimeSpan.FromSeconds(2), "blah"),
                        Recorded.OnNext(TimeSpan.FromSeconds(3), (string)null),
                        Recorded.OnNext(TimeSpan.FromSeconds(4), string.Empty),
                        Recorded.OnNext(TimeSpan.FromSeconds(5), "22.85")
                    },
                    new[]
                    {
                        Recorded.OnNext(TimeSpan.FromSeconds(1), 2100.0),
                        Recorded.OnNext(TimeSpan.FromSeconds(5), 22.85)
                    })
                    .SetName("Emit Power Readings Matching Simple Regex");

                yield return new TestCaseData(
                    @"^{.+""RealPower"":{""Total"":(?<RealPower>\d+(\.\d+)).+}",
                    new[]
                    {
                        Recorded.OnNext(TimeSpan.FromSeconds(1), Resources.Json)
                    },
                    new[]
                    {
                        Recorded.OnNext(TimeSpan.FromSeconds(1), 17.0)
                    })
                    .SetName("Emit Power Readings Matching Advanced Regex");
            }
        }

        [Test, TestCaseSource(nameof(PowerReadings))]
        public async Task EmitThePowerReadingWhenAMessageIsReceivedOnThe(
            string powerReadingPayloadRegex,
            IEnumerable<Recorded<Notification<string>>> source,
            IEnumerable<Recorded<Notification<double>>> expected)
        {
            const string topic = "powerReadings";

            var scheduler = new TestScheduler();

            var config = new PowerFull.Messaging.Config
            {
                PowerReadingTopic = topic,
                PowerReadingPayloadValueRegex = powerReadingPayloadRegex
            };
            
            (var messages, var mqttClient, var subject) = await CreateSubject(config: config, devices: new[] { DeviceA, DeviceB });

            scheduler.CreateColdObservable(source.ToArray())
                .Select(payload => new MqttApplicationMessage(topic, payload == null ? null : Encoding.UTF8.GetBytes(payload)))
                .Subscribe(messages);

            var lastMessage = source.Select(notification => TimeSpan.FromTicks(notification.Time)).Max();

            var actual = scheduler.Start(() => subject.RealPower, lastMessage);

            actual.Messages.AssertEqual(expected);
        }
    }
}
