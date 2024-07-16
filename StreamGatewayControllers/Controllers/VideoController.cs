using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StreamGatewayContracts.IntegrationContracts;
using StreamGatewayContracts.IntegrationContracts.Video;
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

        public VideoController(
            ILogger<VideoController> logger,
            IVideoStreamContract videoStreamService,
            IVideoUploadContract videoUploadService,
            IContentMetadataContract contentMetadataContract)
        {
            _logger = logger;
            _videoStreamService = videoStreamService;
            _videoUploadService = videoUploadService;
            _contentMetadataContract = contentMetadataContract;
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
                _logger.LogError($"An error occurred while gettting image stream. Error message: {ex.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, $"Internal server error: {ex.Message}");
            }
            
        }

        [HttpPost("{contentId}")]
        public async Task<IActionResult> UploadVideo([FromRoute] Guid contentId, [FromForm(Name = "file")] IFormFile formFile)
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

            await _contentMetadataContract.SetVideoUploadStateAsync(contentId, UploadState.InProgress);

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    await formFile.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    await _videoUploadService.UploadVideoAsync(contentId.ToString(), memoryStream);
                }

                _logger.LogInformation("Video uploaded successfully for contentId: {ContentId}", contentId);
                await _contentMetadataContract.SetVideoUploadStateAsync(contentId, UploadState.Success);

                return Ok("File uploaded successfully");
            }
            catch (ConflictException ex)
            {
                //TODO: Change state to failed?? Maybe not.. maybe log Fatal or something
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                await _contentMetadataContract.SetVideoUploadStateAsync(contentId, UploadState.Failed);

                _logger.LogError($"An error occurred while uploading video. Error message: {ex.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, $"An error occurred while uploading video. Error message: {ex.Message}");
            }
            //catch (TaskCanceledException)
            //{

            //}
        }

        [HttpDelete("{contentId}")]
        public async Task<IActionResult> RemoveVideo([FromRoute] Guid contentId)
        {
            _logger.LogInformation("Start remove image.");
            try
            {
                await _videoUploadService.RemoveVideoAsync(contentId.ToString());
                await _contentMetadataContract.SetVideoUploadStateAsync(contentId, UploadState.NoFile);
                _logger.LogInformation("Image removed successfully!");
                return Ok("Image removed successfully!");
            }
            catch (FileNotFoundException)
            {
                return NotFound("Image file not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while removing image. Error message: {ex.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, $"An error occurred while removing image. Error message: {ex.Message}");
            }
        }
    }
}

