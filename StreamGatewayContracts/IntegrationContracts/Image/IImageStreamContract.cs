namespace StreamGatewayContracts.IntegrationContracts.Image
{
    public interface IImageStreamContract
    {
        public Stream GetImageStream(string fileName);
    }

}
