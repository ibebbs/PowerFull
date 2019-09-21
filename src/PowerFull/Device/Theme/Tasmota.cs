using System;
using System.Collections.Generic;
using System.Text;

namespace PowerFull.Device.Theme
{
    public class Tasmota : ITheme
    {
        public string PowerStateRequestTopic => "cmnd/%deviceId%/POWER";

        public string PowerStateRequestPayload => null;

        public string PowerStateResponseTopic => "stat/%deviceId%/POWER";

        public string PowerStateResponseOnPayloadRegex => "ON";

        public string PowerStateResponseOffPayloadRegex => "OFF";

        public string PowerOnRequestTopic => "cmnd/%deviceId%/POWER";

        public string PowerOnRequestPayload => "ON";

        public string PowerOffRequestTopic => "cmnd/%deviceId%/POWER";

        public string PowerOffRequestPayload => "OFF";
    }
}
