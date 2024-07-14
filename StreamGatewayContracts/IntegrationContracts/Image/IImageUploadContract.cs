namespace StreamGatewayContracts.IntegrationContracts.Image
{
    public interface IImageUploadContract
    {
        Task UploadImageAsync(string fileName, Stream fileStream);
    }

}
