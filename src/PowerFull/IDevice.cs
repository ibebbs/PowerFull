namespace PowerFull
{
    public interface IDevice
    {
        string Id { get; set; }

        string PowerStateRequestTopic { get; set; }

        string PowerStateRequestPayload { get; set; }

        string PowerStateResponseTopic { get; set; }
        
        string PowerStateResponseOnPayloadRegex { get; set; }
        
        string PowerStateResponseOffPayloadRegex { get; set; }

        string PowerOnRequestTopic { get; set; }

        string PowerOnRequestPayload { get; set; }

        string PowerOffRequestTopic { get; set; }

        string PowerOffRequestPayload { get; set; }
    }
}
