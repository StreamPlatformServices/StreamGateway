using EncryptionService;
using Microsoft.Extensions.Logging;
using StreamGatewayContracts.IntegrationContracts;
using StreamGatewayCoreUtilities.CommonExceptions;
using System.Runtime.InteropServices;

namespace StreamGateway.Services.Implementations
{
    public class FileUploader : IFileUploader
    {
        private readonly ILogger<FileUploader> _logger;
        private readonly string _baseDirectory;

        public FileUploader(
            ILogger<FileUploader> logger)
        {
            _logger = logger;
            _baseDirectory = Directory.GetCurrentDirectory(); //TODO: From config? Maybe it will be in another service so it is no need to change on development state
        }

        public async Task RemoveFileAsync(string folderName, string fileName)
        {
            var fileDirectory = Path.Combine(_baseDirectory, folderName);
            var filePath = Path.Combine(fileDirectory, fileName);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"Attempted to delete non-existing file. File path: {filePath}");
                throw new NotFoundException("The file does not exist.");
            }

            try
            {
                await DeleteFileAsync(filePath);
                //await Task.Run(() => File.Delete(filePath));
                _logger.LogInformation($"File successfully deleted. File path: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while trying to delete file. File path: {filePath}");
                throw;
            }
        }


        //TODO: Rozkmin korzystanie z natywnych funkcji windowsa
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteFile(string lpFileName);

        private Task DeleteFileAsync(string filePath)
        {
            //TODO: niestety SO nie zapewnia asynchronicznych metod usuwania pliku, wiec trzeba tworzyc watek
            //TODO: Rozkmin dzialanie TaskCompletionSource
            return Task.Run(() =>
            {
                if (!DeleteFile(filePath))
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }
            });
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
