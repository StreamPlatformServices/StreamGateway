using Microsoft.Extensions.Options;
using StreamGatewayContracts.IntegrationContracts;
using System.Configuration;

namespace StreamGateway.Services.Interfaces
{
    public class UriService : IUriContract
    {
        private const string VIDEO_ENDPOINT  = "video";
        private const string IMAGE_ENDPOINT  = "image";

        //TODO: From config
        private const string SCHEMA      = "https"; 
        private const string IP_ADDRESS  = "localhost"; 
        private const int    PORT_NUMBER = 7213; 

        //----------------------

        public Uri GetImageStreamUri(Guid contentId)
        {
            try
            {
                return new Uri($"{SCHEMA}://{IP_ADDRESS}:{PORT_NUMBER}/{IMAGE_ENDPOINT}/{contentId}");
            }
            catch (UriFormatException ex)
            {
                throw new ConfigurationException("Invalid URI configuration", ex);
            }
        }
        
        public Uri GetVideoStreamUri(Guid contentId)
        {
            try
            {
                return new Uri($"{SCHEMA}://{IP_ADDRESS}:{PORT_NUMBER}/{VIDEO_ENDPOINT}/{contentId}");
            }
            catch (UriFormatException ex)
            {
                throw new ConfigurationException("Invalid URI configuration", ex);
            }
        }
    }

}
