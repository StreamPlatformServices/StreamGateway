using EncryptionService;
using KeyServiceAPI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
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
        private readonly IFileDecryptor _fileDecryptor;
        private readonly IKeyServiceClient _keyServiceClient;

        

        public VideoController(
            ILogger<VideoController> logger,
            IVideoStreamContract videoStreamService,
            IVideoUploadContract videoUploadService,
            IContentMetadataContract contentMetadataContract,
            IFileEncryptor fileEncryptor,
            IFileDecryptor fileDecryptor,
            IKeyServiceClient keyServiceClient)
        {
            _logger = logger;
            _videoStreamService = videoStreamService;
            _videoUploadService = videoUploadService;
            _contentMetadataContract = contentMetadataContract;
            _fileEncryptor = fileEncryptor;
            _fileDecryptor = fileDecryptor;
            _keyServiceClient = keyServiceClient;
        }
        //TODO: Maybe make this endpoints less obvious??
        [HttpGet("{contentId}")] 
        public IActionResult GetVideoStream([FromRoute] Guid contentId)
        {
            _logger.LogInformation("Start get video stream procedure.");
            try
            {
                var videoStream = _videoStreamService.GetVideoStream(contentId.ToString());

                Response.Headers.Add("Accept-Ranges", "bytes");
                //Response.Headers.Add("Content-Disposition", "inline; filename=\"video.mp4\""); //TODO: remove
                Response.Headers.Add("Content-Type", "video/webm; codecs=\"vp8, vorbis\"");

                return File(videoStream, "video/webm; codecs=\"vp8, vorbis\"");
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

        //TODO: it's a development test endpoint
        [HttpPost("decrypt/{videoFileId}")]
        public async Task<IActionResult> DecryptVideo([FromRoute] Guid videoFileId)
        {   
            var response = new ResponseModel<VideoUploadResponseModel> { Result = new VideoUploadResponseModel() };

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "videos", $"{videoFileId}.mp4");

            var tempFilePath = Path.GetTempFileName();

            try
            {
                using (var decryptedFileStream = new FileStream(tempFilePath, FileMode.Create))
                {
                    using (var inputStream = new FileStream(filePath, FileMode.Open))
                    {
                        await _fileDecryptor.DecryptAES(videoFileId, inputStream, decryptedFileStream);
                    }
                }

                using (var decryptedFileStream = new FileStream(tempFilePath, FileMode.Open))
                {
                    await _videoUploadService.UploadVideoAsync("DECRYPTED", decryptedFileStream);
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

                //try //TODO: Move the try catch to a private method: RollBackEncryptionKey or somethingLikeThis
                //{
                //    await _keyServiceClient.DeleteEncryptionKeyAsync(videoFileId);
                //}
                //catch (Exception deleteEx)
                //{
                //    _logger.LogError($"An error occurred while deleting encryption key. Error message: {deleteEx.Message}");
                //}

                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                response.Message = ex.ToString();
                _logger.LogError($"An error occurred while uploading video. Error message: {ex.Message}");

                //try
                //{
                //    await _keyServiceClient.DeleteEncryptionKeyAsync(videoFileId);
                //}
                //catch (Exception deleteEx)
                //{
                //    _logger.LogError($"An error occurred while deleting encryption key. Error message: {deleteEx.Message}");
                //}

                return StatusCode((int)HttpStatusCode.InternalServerError, $"An error occurred while uploading video. Error message: {ex.Message}");
            }
            finally
            {
                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }
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

            await _keyServiceClient.CreateEncryptionKeyAsync(videoFileId); //TODO: THink where should it be called!!! and get encryption Key also

            var response = new ResponseModel<VideoUploadResponseModel> { Result = new VideoUploadResponseModel() };

            var tempFilePath = Path.GetTempFileName();

            try
            {
                //TODO: It was working on memmory stream (the issue was not enough space)
                using (var encryptedFileStream = new FileStream(tempFilePath, FileMode.Create))
                {
                    using (var inputStream = formFile.OpenReadStream())
                    {
                        await _fileEncryptor.EncryptAES(videoFileId, inputStream, encryptedFileStream);
                    }
                }

                // TODO: decouple this functionality:
                // 1. creating temp file <encryptionService> 
                // 2. accept file by administrator <encryption service?>
                // 3. save file in repository (encryptionService->streamService))

                using (var encryptedFileStream = new FileStream(tempFilePath, FileMode.Open))
                {
                    await _videoUploadService.UploadVideoAsync(videoFileId.ToString(), encryptedFileStream);
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

                try //TODO: Move the try catch to a private method: RollBackEncryptionKey or somethingLikeThis
                {
                    await _keyServiceClient.DeleteEncryptionKeyAsync(videoFileId);
                }
                catch (Exception deleteEx)
                {
                    _logger.LogError($"An error occurred while deleting encryption key. Error message: {deleteEx.Message}");
                }

                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                response.Message = ex.ToString();
                _logger.LogError($"An error occurred while uploading video. Error message: {ex.Message}");

                try
                {
                    await _keyServiceClient.DeleteEncryptionKeyAsync(videoFileId);
                }
                catch (Exception deleteEx)
                {
                    _logger.LogError($"An error occurred while deleting encryption key. Error message: {deleteEx.Message}");
                }

                return StatusCode((int)HttpStatusCode.InternalServerError, $"An error occurred while uploading video. Error message: {ex.Message}");
            }
            finally
            {
                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }
            }

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

