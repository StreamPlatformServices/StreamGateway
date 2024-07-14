using Microsoft.Extensions.Logging;
using StreamGatewayContracts.IntegrationContracts;
using StreamGatewayCoreUtilities.CommonExceptions;

namespace StreamGateway.Services.Implementations
{
    public class FileUploader : IFileUploader
    {
        private readonly ILogger<FileUploader> _logger;
        private readonly string _baseDirectory;

        public FileUploader(ILogger<FileUploader> logger)
        {
            _logger = logger;
            _baseDirectory = Directory.GetCurrentDirectory(); //TODO: From config? Maybe it will be in another service so it is no need to change on development state
        }

        public async Task UploadFileAsync(string folderName, string fileName, Stream fileStream)
        {
            var fileDirectory = Path.Combine(_baseDirectory, folderName);

            if (!Directory.Exists(fileDirectory))
            {
                Directory.CreateDirectory(fileDirectory);
            }

            var filePath = Path.Combine(fileDirectory, fileName);

            if (File.Exists(filePath))
            {
                _logger.LogError($"Try to save content which already exist. File path: {filePath}");
                throw new ConflictException("A file with the same name already exists.");
            }

            using (var fileStreamOutput = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await fileStream.CopyToAsync(fileStreamOutput);
            }
        }
    }
}
