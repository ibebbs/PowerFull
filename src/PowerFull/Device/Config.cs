namespace PowerFull.Device
{
    public class Config
    {
        public string Theme { get; set; }
        public string PowerStateRequestTopic { get; set; }
        public string PowerStateRequestPayload { get; set; }
        public string PowerStateResponseTopic { get; set; }
        public string PowerStateResponseOnPayloadRegex { get; set; }
        public string PowerStateResponseOffPayloadRegex { get; set; }
        public string PowerOnRequestTopic { get; set; }
        public string PowerOnRequestPayload { get; set; }
        public string PowerOffRequestTopic { get; set; }
        public string PowerOffRequestPayload { get; set; }
    }
}
