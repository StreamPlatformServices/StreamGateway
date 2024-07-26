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
        private const string SCHEMA      = "https"; 
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
                return new Uri($"{SCHEMA}://{_kestrelSettings.ListeningIPv4Address}:{_kestrelSettings.TlsPortNumber}/{IMAGE_ENDPOINT}/{contentId}");
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
                return new Uri($"{SCHEMA}://{_kestrelSettings.ListeningIPv4Address}:{_kestrelSettings.TlsPortNumber}/{VIDEO_ENDPOINT}/{contentId}");
            }
            catch (UriFormatException ex)
            {
                throw new ConfigurationException("Invalid URI configuration", ex);
            }
        }
    }

}
