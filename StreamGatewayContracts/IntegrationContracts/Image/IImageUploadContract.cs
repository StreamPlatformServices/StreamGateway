namespace StreamGatewayContracts.IntegrationContracts.Image
{
    public interface IImageUploadContract
    {
        Task UploadImageAsync(string fileName, Stream fileStream);
        Task RemoveImageAsync(string fileName); //TODO: Different interface?????
    }

}
