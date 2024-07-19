using EncryptionService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StreamGatewayContracts.IntegrationContracts;
using StreamGatewayContracts.IntegrationContracts.Video;
using StreamGatewayControllers.Models;
using StreamGatewayCoreUtilities.CommonExceptions;
using System.Net;

namespace StreamGateway.Controllers
{
    [ApiController]
    [Route("video")]
    public class VideoController : ControllerBase
    {
        private readonly ILogger<VideoController> _logger;
        //TODO: Te kontrakty video i image napewno mozna lepiej zaprojektowac!
        private readonly IVideoStreamContract _videoStreamService; //TODO: change name to contract
        private readonly IVideoUploadContract _videoUploadService;
        private readonly IContentMetadataContract _contentMetadataContract;
        private readonly IFileEncryptor _fileEncryptor;

        public VideoController(
            ILogger<VideoController> logger,
            IVideoStreamContract videoStreamService,
            IVideoUploadContract videoUploadService,
            IContentMetadataContract contentMetadataContract,
            IFileEncryptor fileEncryptor)
        {
            _logger = logger;
            _videoStreamService = videoStreamService;
            _videoUploadService = videoUploadService;
            _contentMetadataContract = contentMetadataContract;
            _fileEncryptor = fileEncryptor;
        }

        [HttpGet("{contentId}")] //TODO: Maybe make this endpoints less obvious??
        public IActionResult GetVideoStream([FromRoute] Guid contentId)
        {
            _logger.LogInformation("Start get video stream procedure.");
            try
            {
                var videoStream = _videoStreamService.GetVideoStream(contentId.ToString());
                
                return File(videoStream, "video/mp4");
            }
            catch (FileNotFoundException)
            {
                return NotFound("Video file not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while gettting video stream. Error message: {ex.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, $"Internal server error: {ex.Message}");
            }
            
        }

        [HttpPost("{videoFileId}")]
        public async Task<IActionResult> UploadVideo([FromRoute] Guid videoFileId, [FromForm(Name = "file")] IFormFile formFile)
        {
            //TODO: Check Exception on the middleware side and send video state update to failed!!???? 
            //What types of problems can occur on the middleware or json converter side??????
            if (!Request.HasFormContentType)
            {
                return BadRequest("Unsupported content type");
            }

            if (formFile == null || formFile.Length == 0)
            {
                return BadRequest("File not provided or empty");
            }

            var response = new ResponseModel<VideoUploadResponseModel> { Result = new VideoUploadResponseModel() };

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    await formFile.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    var encryptedStream = await _fileEncryptor.EncryptAES(videoFileId, memoryStream);

                    await _videoUploadService.UploadVideoAsync(videoFileId.ToString(), encryptedStream);
                }

                _logger.LogInformation("Video uploaded successfully file id: {videoFileId}", videoFileId);

                response.Message = "File uploaded successfully";
                response.Result.VideoFileId = videoFileId;

                return Ok(response);
            }
            catch (ConflictException ex)
            {
                //TODO: maybe log Fatal or something
                response.Message = ex.Message;
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                response.Message = ex.ToString();
                _logger.LogError($"An error occurred while uploading video. Error message: {ex.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, $"An error occurred while uploading video. Error message: {ex.Message}");
            }
            //catch (TaskCanceledException)
            //{

            //}
        }

        [HttpDelete("{contentId}")]
        public async Task<IActionResult> RemoveVideo([FromRoute] Guid fileId)
        {
            _logger.LogInformation("Start remove video.");
            try
            {
                await _videoUploadService.RemoveVideoAsync(fileId.ToString());
                await _contentMetadataContract.SetVideoUploadStateAsync(fileId, UploadState.NoFile);
                _logger.LogInformation("Video removed successfully!");
                return Ok("Video removed successfully!");
            }
            catch (FileNotFoundException)
            {
                return NotFound("Video file not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while removing video. Error message: {ex.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, $"An error occurred while removing video. Error message: {ex.Message}");
            }
        }
    }
}

