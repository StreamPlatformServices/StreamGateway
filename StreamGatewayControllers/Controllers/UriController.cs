using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StreamGatewayContracts.IntegrationContracts;
using StreamGatewayControllers.Models;
using System.Net;

namespace StreamGateway.Controllers
{
    [ApiController]
    [Route("uri")]
    public class UriController : ControllerBase
    {
        private readonly ILogger<UriController> _logger;
        private readonly IUriContract _uriService;

        public UriController(
            ILogger<UriController> logger,
            IUriContract urlService)
        {
            _logger = logger;
            _uriService = urlService;
        }

        [HttpGet("video/{contentId}")]
        public IActionResult GetVideoStreamUri([FromRoute] Guid contentId)
        {
            var response = new ResponseModel<GetUriResponseModel> { Result = new GetUriResponseModel() };
            try 
            {
                var url = _uriService.GetVideoStreamUri(contentId).AbsoluteUri;
                response.Result.Url = url;
                response.Result.LifeTime = UrlLifeTime.Infinit; //TODO: Config ??
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while getting the video stream uri. Error message: {ex.Message}");
                response.Message = $"An error occurred while getting the video stream uri. Error message: {ex.Message}";
                return StatusCode((int)HttpStatusCode.InternalServerError, response);
            }
        }

        [HttpGet("image/{contentId}")]
        public IActionResult GetImageStreamUri([FromRoute] Guid contentId)
        {
            var response = new ResponseModel<GetUriResponseModel> { Result = new GetUriResponseModel() };
            try
            {
                var url = _uriService.GetImageStreamUri(contentId).AbsoluteUri;
                response.Result.Url = url;
                response.Result.LifeTime = UrlLifeTime.Infinit; //TODO: Config ?? //TODO: make it Once or time limited
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while getting the video stream uri. Error message: {ex.Message}");
                response.Message = $"An error occurred while getting the video stream uri. Error message: {ex.Message}";
                return StatusCode((int)HttpStatusCode.InternalServerError, response);
            }
        }
    }
}
