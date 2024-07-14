namespace StreamGatewayContracts.IntegrationContracts.Video
{
    public interface IVideoUploadContract
    {
        Task UploadVideoAsync(string fileName, Stream fileStream);
    }

}
