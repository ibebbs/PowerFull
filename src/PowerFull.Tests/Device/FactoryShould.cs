using FakeItEasy;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PowerFull.Tests.Device
{
    using Device = PowerFull.Device;

    [TestFixture]
    public class FactoryShould
    {
        [Test]
        public async Task CorrectlyBuildADeviceFromATheme()
        {
            var config = new Device.Config
            {
                Theme = "tasmota"
            };

            var options = A.Fake<IOptions<Device.Config>>();
            A.CallTo(() => options.Value).Returns(config);

            var device = await new Device.Factory(options).CreateDevice("test");

            Assert.That(device.Id, Is.EqualTo("test"));

            Assert.That(device.PowerStateRequestTopic, Is.EqualTo("cmnd/test/POWER"));
            Assert.That(device.PowerStateRequestPayload, Is.EqualTo(null));
            Assert.That(device.PowerStateResponseTopic, Is.EqualTo("stat/test/POWER"));
            Assert.That(device.PowerStateResponseOnPayloadRegex, Is.EqualTo("ON"));
            Assert.That(device.PowerStateResponseOffPayloadRegex, Is.EqualTo("OFF"));
            Assert.That(device.PowerOnRequestTopic, Is.EqualTo("cmnd/test/POWER"));
            Assert.That(device.PowerOnRequestPayload, Is.EqualTo("ON"));
            Assert.That(device.PowerOffRequestTopic, Is.EqualTo("cmnd/test/POWER"));
            Assert.That(device.PowerOffRequestPayload, Is.EqualTo("OFF"));
        }

        [Test]
        public async Task CorrectlyBuildADeviceFromConfig()
        {
            var config = new Device.Config
            {
                PowerStateRequestTopic = "cmnd/%deviceId%/POWER",
                PowerStateRequestPayload = null,
                PowerStateResponseTopic = "stat/%deviceId%/POWER",
                PowerStateResponseOnPayloadRegex = "ON",
                PowerStateResponseOffPayloadRegex = "OFF",
                PowerOnRequestTopic = "cmnd/%deviceId%/POWER",
                PowerOnRequestPayload = "ON",
                PowerOffRequestTopic = "cmnd/%deviceId%/POWER",
                PowerOffRequestPayload = "OFF"
            };

            var options = A.Fake<IOptions<Device.Config>>();
            A.CallTo(() => options.Value).Returns(config);

            var device = await new Device.Factory(options).CreateDevice("test");

            Assert.That(device.Id, Is.EqualTo("test"));

            Assert.That(device.PowerStateRequestTopic, Is.EqualTo("cmnd/test/POWER"));
            Assert.That(device.PowerStateRequestPayload, Is.EqualTo(null));
            Assert.That(device.PowerStateResponseTopic, Is.EqualTo("stat/test/POWER"));
            Assert.That(device.PowerStateResponseOnPayloadRegex, Is.EqualTo("ON"));
            Assert.That(device.PowerStateResponseOffPayloadRegex, Is.EqualTo("OFF"));
            Assert.That(device.PowerOnRequestTopic, Is.EqualTo("cmnd/test/POWER"));
            Assert.That(device.PowerOnRequestPayload, Is.EqualTo("ON"));
            Assert.That(device.PowerOffRequestTopic, Is.EqualTo("cmnd/test/POWER"));
            Assert.That(device.PowerOffRequestPayload, Is.EqualTo("OFF"));
        }
        [Test]
        public async Task CorrectlyBuildADeviceFromConfigAndTheme()
        {
            var config = new Device.Config
            {
                Theme = "tasmota",
                PowerStateRequestTopic = "%deviceId%/cmnd/POWER"
            };

            var options = A.Fake<IOptions<Device.Config>>();
            A.CallTo(() => options.Value).Returns(config);

            var device = await new Device.Factory(options).CreateDevice("test");

            Assert.That(device.Id, Is.EqualTo("test"));

            Assert.That(device.PowerStateRequestTopic, Is.EqualTo("test/cmnd/POWER"));
            Assert.That(device.PowerStateRequestPayload, Is.EqualTo(null));
            Assert.That(device.PowerStateResponseTopic, Is.EqualTo("stat/test/POWER"));
            Assert.That(device.PowerStateResponseOnPayloadRegex, Is.EqualTo("ON"));
            Assert.That(device.PowerStateResponseOffPayloadRegex, Is.EqualTo("OFF"));
            Assert.That(device.PowerOnRequestTopic, Is.EqualTo("cmnd/test/POWER"));
            Assert.That(device.PowerOnRequestPayload, Is.EqualTo("ON"));
            Assert.That(device.PowerOffRequestTopic, Is.EqualTo("cmnd/test/POWER"));
            Assert.That(device.PowerOffRequestPayload, Is.EqualTo("OFF"));
        }
    }
}
