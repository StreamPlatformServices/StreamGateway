namespace StreamGatewayContracts.IntegrationContracts
{
    public interface IFileUploader
    {
        Task UploadFileAsync(string folderName, string fileName, Stream fileStream);
    }

}
