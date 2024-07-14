using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StreamGatewayContracts.IntegrationContracts;
using StreamGatewayContracts.IntegrationContracts.Image;
using StreamGatewayCoreUtilities.CommonExceptions;
using System.Net;

namespace StreamGateway.Controllers
{
    [ApiController]
    [Route("image")]
    public class ImageController : ControllerBase
    {
        private readonly ILogger<ImageController> _logger;
        private readonly IImageStreamContract _imageStreamService;
        private readonly IImageUploadContract _imageUploadService;
        private readonly IContentMetadataContract _contentMetadataContract;

        public ImageController(
            ILogger<ImageController> logger,
            IImageStreamContract imageStreamService,
            IImageUploadContract imageUploadService,
            IContentMetadataContract contentMetadataContract)
        {
            _logger = logger;
            _imageStreamService = imageStreamService;
            _imageUploadService = imageUploadService;
            _contentMetadataContract = contentMetadataContract;
        }

        [HttpGet("{contentId}")]
        public IActionResult StreamImage([FromRoute] Guid contentId)
        {
            try
            {
                //FileStream is created with flag useAsync which enables thread non-blocking i/o operations and streaming operations //TODO: test it!!! 
                var fileStream = _imageStreamService.GetImageStream(contentId.ToString());
                return File(fileStream, "image/jpeg");
            }
            catch (FileNotFoundException)
            {
                return NotFound("Image file not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while gettting image stream. Error message: {ex.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, $"An error occurred while gettting image stream. Error message: {ex.Message}");
            }
        }

        [HttpPost("{contentId}")]
        public async Task<IActionResult> UploadImage([FromRoute] Guid contentId, [FromForm(Name = "file")] IFormFile formFile)
        {
            if (!Request.HasFormContentType)
            {
                return BadRequest("Unsupported content type");
            }

            if (formFile == null || formFile.Length == 0)
            {
                return BadRequest("File not provided or empty");
            }

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    await formFile.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    await _imageUploadService.UploadImageAsync(contentId.ToString(), memoryStream);
                }

                _logger.LogInformation("Image uploaded successfully for contentId: {ContentId}", contentId);
                await _contentMetadataContract.SetImageUploadStateAsync(contentId, UploadState.Success);
                return Ok("File uploaded successfully");
            }
            catch (ConflictException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while uploading image. Error message: {ex.Message}");
                await _contentMetadataContract.SetImageUploadStateAsync(contentId, UploadState.Failed);
                return StatusCode((int)HttpStatusCode.InternalServerError, $"An error occurred while uploading video. Error message: {ex.Message}");
            }
            //catch (TaskCanceledException)
            //{
                
            //}

        }

        //[HttpPost("upload")]
        //public async Task<IActionResult> UploadImage(Guid contentId)
        //{

        //    if (!Request.HasFormContentType)
        //    {
        //        return BadRequest("Unsupported content type");
        //    }

        //    var form = await Request.ReadFormAsync();
        //    var file = form.Files.GetFile("file");

        //    if (file == null || file.Length == 0)
        //    {
        //        return BadRequest("File not provided or empty");
        //    }

        //    using (var fileStream = file.OpenReadStream())
        //    {
        //        await _imageUploadService.UploadJpgImageAsync(contentId.ToString(), fileStream);
        //    }

        //    return Ok("File uploaded successfully");
        //}

        //[HttpPost("upload")]
        //public async Task<IActionResult> UploadImage(Guid contentId)
        //{

        //    if (!Request.HasFormContentType)
        //    {
        //        return BadRequest("Unsupported content type");
        //    }

        //    using (var fileStream = new MemoryStream())
        //    {
        //        await Request.Body.CopyToAsync(fileStream);
        //        fileStream.Position = 0; 

        //        await _imageUploadService.UploadJpgImageAsync(contentId.ToString(), fileStream);
        //    }

        //    return Ok("File uploaded successfully");
        //}


    }
}
