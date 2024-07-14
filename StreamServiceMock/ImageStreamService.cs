using Microsoft.Extensions.Logging;
using StreamGateway.Services.Implementations;
using StreamGatewayContracts.IntegrationContracts.Image;

namespace StreamGateway.Services.Interfaces
{
    public class ImageStreamService : IImageStreamContract
    {
        //TODO: Separate ContentMetadataId from file names. Create database wich will map contentId with image file name and movie file name
        private readonly ILogger<ImageStreamService> _logger;
        private readonly string _imageDirectory;
        private const int BUFFER_SIZE = 4096;

        public ImageStreamService(ILogger<ImageStreamService> logger)
        {
            _logger = logger;
            _imageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "images"); //TODO: config
        }

        //TODO: Pokombinuj z generycznością formatu plików, czy osobna klasa do formatów jpg czy może jeszcze inaczej
        public Stream GetImageStream(string fileName) 
        {
            var filePath = Path.Combine(_imageDirectory, $"{fileName}.jpg");
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Image not found.");
            }

            try
            {
                return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BUFFER_SIZE, useAsync: true);
            }
            catch (IOException ex)
            {
                _logger.LogError($"An error occurred while trying to open the file. Error Message: {ex.Message}");
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

    }
}
