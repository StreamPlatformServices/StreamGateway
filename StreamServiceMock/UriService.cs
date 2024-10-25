using APIGatewayCoreUtilities.CommonConfiguration.ConfigurationModels;
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
        private const string SCHEMA      = "http"; 
        private const string SCHEMA_SSL      = "https"; 
        //----------------------

        private readonly KestrelSettings _kestrelSettings;

        public UriService(IOptions<KestrelSettings> options)
        {
            _kestrelSettings = options.Value;
        }

        public Uri GetImageStreamUri(Guid contentId)
        {
            
            try
            {
                if (_kestrelSettings.UseTls)
                {
                    return new Uri($"{SCHEMA_SSL}://{_kestrelSettings.StreamingIPv4Address}:{_kestrelSettings.TlsPortNumber}/{IMAGE_ENDPOINT}/{contentId}");
                }

                return new Uri($"{SCHEMA}://{_kestrelSettings.StreamingIPv4Address}:{_kestrelSettings.PortNumber}/{IMAGE_ENDPOINT}/{contentId}");

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
                if (_kestrelSettings.UseTls)
                {
                    return new Uri($"{SCHEMA_SSL}://{_kestrelSettings.StreamingIPv4Address}:{_kestrelSettings.TlsPortNumber}/{VIDEO_ENDPOINT}/{contentId}");
                }

                return new Uri($"{SCHEMA}://{_kestrelSettings.StreamingIPv4Address}:{_kestrelSettings.PortNumber}/{VIDEO_ENDPOINT}/{contentId}");
                
            }
            catch (UriFormatException ex)
            {
                throw new ConfigurationException("Invalid URI configuration", ex);
            }
        }
    }

}
