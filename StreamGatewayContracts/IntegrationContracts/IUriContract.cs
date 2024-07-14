using System.Net;

namespace StreamGatewayContracts.IntegrationContracts
{
    public interface IUriContract
    {
       Uri GetImageStreamUri(Guid contentId);
       Uri GetVideoStreamUri(Guid contentId);
    }
}
