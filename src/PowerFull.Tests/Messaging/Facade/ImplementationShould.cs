using FakeItEasy;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mqtt;
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

        [Test]
        public async Task SubscribeToExpectedTopics()
        {
            var config = new PowerFull.Messaging.Config
            {
                PowerReadingTopic = "TestPowerReadingTopic"
            };

            var mqttClient = A.Fake<IMqttClient>();
            var mqttFactory = A.Fake<PowerFull.Messaging.Mqtt.IFactory>();
            A.CallTo(() => mqttFactory.Create()).Returns(new ValueTask<IMqttClient>(mqttClient));

            var subject = new PowerFull.Messaging.Facade.Implementation(config, mqttFactory, new[] { DeviceA, DeviceB });
            await subject.InitializeAsync();

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
            var config = new PowerFull.Messaging.Config();
            var messages = new Subject<MqttApplicationMessage>();
            var mqttClient = A.Fake<IMqttClient>();
            A.CallTo(() => mqttClient.MessageStream).Returns(messages);
            var mqttFactory = A.Fake<PowerFull.Messaging.Mqtt.IFactory>();
            A.CallTo(() => mqttFactory.Create()).Returns(new ValueTask<IMqttClient>(mqttClient));

            var subject = new PowerFull.Messaging.Facade.Implementation(config, mqttFactory, new[] { DeviceA, DeviceB });
            await subject.InitializeAsync();

            subject.GetPowerState(DeviceA);

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
            var config = new PowerFull.Messaging.Config();
            var messages = new Subject<MqttApplicationMessage>();
            var mqttClient = A.Fake<IMqttClient>();
            A.CallTo(() => mqttClient.MessageStream).Returns(messages);
            var mqttFactory = A.Fake<PowerFull.Messaging.Mqtt.IFactory>();
            A.CallTo(() => mqttFactory.Create()).Returns(new ValueTask<IMqttClient>(mqttClient));

            var subject = new PowerFull.Messaging.Facade.Implementation(config, mqttFactory, new[] { DeviceA, DeviceB });
            await subject.InitializeAsync();

            subject.GetPowerState(DeviceA);

            A.CallTo(
                () => mqttClient.PublishAsync(
                    A<MqttApplicationMessage>.That.Matches(message => Encoding.UTF8.GetString(message.Payload).Equals("Whats the frequency Kenneth?")),
                    A<MqttQualityOfService>.Ignored,
                    false))
             .MustHaveHappenedOnceExactly();
        }
    }
}
