namespace StreamGatewayContracts.IntegrationContracts
{
    public interface IFileUploader
    {
        Task UploadFileAsync(string folderName, string fileName, Stream fileStream);
        Task RemoveFileAsync(string folderName, string fileName);
    }

}
