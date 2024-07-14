using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StreamGatewayContracts.IntegrationContracts;
using StreamGatewayCoreUtilities.CommonExceptions;
using System.Net;
using System.Text;

namespace ContentMetadataServiceAPI
{
    public class ContentMetadataContract : IContentMetadataContract
    {

        private readonly HttpClient _httpClient;
        private readonly ILogger<ContentMetadataContract> _logger;

        private const string BASE_ENDPOINT = "uri/temp"; //TODO: will be changed (when implementation of content metadata service will be finished)
        private const string VIDEO_ENDPOINT = "video";
        private const string IMAGE_ENDPOINT = "image";

        public ContentMetadataContract(
            ILogger<ContentMetadataContract> logger,
            HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task SetImageUploadStateAsync(Guid contentId, UploadState uploadState)
        {
            //TODO: token!

            var requestContent = new StringContent(
                            JsonConvert.SerializeObject(new { uploadState }),
                            Encoding.UTF8,
                            "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{BASE_ENDPOINT}/{IMAGE_ENDPOINT}/{contentId}", requestContent);

                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                HandleStatusCode(response);
                _logger.LogError($"Unexpected error in response while trying to change upload state for video file. Message: {response.ReasonPhrase}");
                throw new Exception(response.ReasonPhrase);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error occurred while trying to change upload state for video file.");
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error occurred while trying to change upload state for video file.");
                throw;
            }
        }

        public async Task SetVideoUploadStateAsync(Guid contentId, UploadState uploadState)
        {
            //TODO: token!

            var requestContent = new StringContent(
                            JsonConvert.SerializeObject(new { uploadState }),
                            Encoding.UTF8,
                            "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{BASE_ENDPOINT}/{VIDEO_ENDPOINT}/{contentId}", requestContent);

                if (response.IsSuccessStatusCode)
                {
                    return;
                }
                //TODO: if not found remove file?

                HandleStatusCode(response);
                _logger.LogError($"Unexpected error in response while trying to change upload state for image file. Message: {response.ReasonPhrase}");
                throw new Exception(response.ReasonPhrase);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error occurred while trying to change upload state for image file.");
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error occurred while trying to change upload state for image file.");
                throw;
            }
        }

        private void HandleStatusCode(HttpResponseMessage response)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.NotFound: //TODO: Is NotFound needed in this place??
                    throw new NotFoundException(response.ReasonPhrase);
                case HttpStatusCode.Unauthorized:
                    _logger.LogWarning("Request should be blocked in APIGateway middleware!"); //TODO: should be autho in strem gateway???
                    throw new UnauthorizedException(response.ReasonPhrase);
                case HttpStatusCode.Forbidden:
                    _logger.LogWarning("Request should be blocked in APIGateway middleware!");
                    throw new ForbiddenException(response.ReasonPhrase);
            }
        }
    }
}
