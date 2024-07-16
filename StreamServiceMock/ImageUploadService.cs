using Microsoft.Extensions.Logging;
using StreamGatewayContracts.IntegrationContracts;
using StreamGatewayContracts.IntegrationContracts.Image;

namespace StreamGateway.Services.Implementations
{
    public class ImageUploadService : IImageUploadContract
    {
        private readonly ILogger<ImageUploadService> _logger;
        private readonly IFileUploader _fileUploader;
        
        private const string IMAGE_FOLDER_NAME = "images"; 
        private const string JPG_EXTENSION = ".jpg";  //TODO: function param FileFormat ?

        public ImageUploadService(
            ILogger<ImageUploadService> logger,
            IFileUploader fileUploader)
        {
            _fileUploader = fileUploader;
            _logger = logger;
        }

        public async Task UploadImageAsync(string fileName, Stream fileStream)
        {
            try
            {
                await _fileUploader.UploadFileAsync(IMAGE_FOLDER_NAME, $"{fileName}{JPG_EXTENSION}", fileStream);
            }
            catch (IOException ex)
            {
                _logger.LogError($"An error occurred while uploading the file. Error Message: {ex.Message}");
                throw new IOException("An error occurred while uploading the file.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError($"You do not have permission to upload the file. Error Message: {ex.Message}");
                throw new UnauthorizedAccessException("You do not have permission to upload the file.", ex);
            }
            catch (TaskCanceledException ex)
            {
                //TODO: Handle canceling in streaming and in upload!!!!!!!!!! ???
                throw new TaskCanceledException("The upload task was canceled.", ex);
            }
        }

        public async Task RemoveImageAsync(string fileName)
        {
            await _fileUploader.RemoveFileAsync(IMAGE_FOLDER_NAME, $"{fileName}{JPG_EXTENSION}");
        }
    }
}
