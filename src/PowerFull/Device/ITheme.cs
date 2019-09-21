namespace PowerFull.Device
{
    public interface ITheme
    {
        string PowerStateRequestTopic { get; }
        string PowerStateRequestPayload { get; }
        string PowerStateResponseTopic { get; }
        string PowerStateResponseOnPayloadRegex { get; }
        string PowerStateResponseOffPayloadRegex { get; }
        string PowerOnRequestTopic { get; }
        string PowerOnRequestPayload { get; }
        string PowerOffRequestTopic { get; }
        string PowerOffRequestPayload { get; }
    }
}
