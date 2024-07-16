using System.Net;

namespace StreamGatewayContracts.IntegrationContracts
{
    //TODO: Move to another file???
    public enum UploadState
    {
        NoFile,
        InProgress,
        Success,
        Failed
    }
    public interface IContentMetadataContract
    {
       Task SetImageUploadStateAsync(Guid contentId, UploadState uploadState);
       Task SetVideoUploadStateAsync(Guid contentId, UploadState uploadState);
    }
}
