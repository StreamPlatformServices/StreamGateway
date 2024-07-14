namespace StreamGatewayContracts.IntegrationContracts.Video
{
    public interface IVideoStreamContract
    {
        Stream GetVideoStream(string videoFileName);
    }

}
