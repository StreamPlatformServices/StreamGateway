﻿using Microsoft.Extensions.Logging;
using StreamGatewayContracts.IntegrationContracts.Video;

namespace StreamGateway.Services.Interfaces
{
    public class VideoStreamService : IVideoStreamContract
    {
        private readonly ILogger<VideoStreamService> _logger;
        private readonly string _videoDirectory;
        private const int BUFFER_SIZE = 4096;
        private const string FILE_EXTENSION = ".webm";

        public VideoStreamService(ILogger<VideoStreamService> logger) //TOOD: Create file streamer???
        {
            _videoDirectory = Path.Combine(Directory.GetCurrentDirectory(), "videos"); //TODO: config
            _logger = logger;
        }

        public Stream GetVideoStream(string videoFileName)
        {
            var videoPath = Path.Combine(_videoDirectory, $"{videoFileName}{FILE_EXTENSION}"); //TODO: Configure formats

            if (!File.Exists(videoPath))
            {
                throw new FileNotFoundException("Video file not found", videoFileName);
            }

            try
            {
                return new FileStream(videoPath, FileMode.Open, FileAccess.Read, FileShare.Read, BUFFER_SIZE, useAsync: true);
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
