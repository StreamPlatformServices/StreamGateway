using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StreamGatewayContracts.IntegrationContracts;
using StreamGatewayContracts.IntegrationContracts.Video;
using StreamGatewayCoreUtilities.CommonConfiguration;

namespace StreamGateway.Services.Interfaces
{
    public class VideoUploadService : IVideoUploadContract
    {
        private readonly ILogger<VideoUploadService> _logger;
        private readonly VideoFileSettings _videoFileSettings;
        private readonly IFileUploader _fileUploader;

        private const string VIDEO_FOLDER_NAME = "videos"; //TODO: Config??

        public VideoUploadService(
            ILogger<VideoUploadService> logger,
            IFileUploader fileUploader,
            IOptions<StreamServiceSettings> options)
        {
            _logger = logger;
            _fileUploader = fileUploader;
            _videoFileSettings = options.Value.VideoFileSettings;
        }

        public async Task UploadVideoAsync(string fileName, Stream fileStream)
        {
            try
            {
                await _fileUploader.UploadFileAsync(VIDEO_FOLDER_NAME, $"{fileName}.{_videoFileSettings.FileFormat}", fileStream);
            }
            catch (IOException ex)
            {
                //TODO: Move all exceptions to Upload File
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

        public async Task RemoveVideoAsync(string fileName)
        {
            await _fileUploader.RemoveFileAsync(VIDEO_FOLDER_NAME, $"{fileName}.{_videoFileSettings.FileFormat}");
        }
    }
}
