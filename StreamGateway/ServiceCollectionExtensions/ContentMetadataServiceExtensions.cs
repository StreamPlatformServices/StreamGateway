using APIGatewayCoreUtilities.CommonConfiguration.ConfigurationModels;
using ContentMetadataServiceAPI;
using Microsoft.Extensions.Options;
using StreamGatewayContracts.IntegrationContracts;


namespace StreamGatewayMain.ServiceCollectionExtensions
{

    public static class ContentMetadataServiceExtensions
    {
        public static IServiceCollection AddContentMetadataServiceAPI(this IServiceCollection services)
        {
            services.AddHttpClient<IContentMetadataContract, ContentMetadataContract>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<ContentMetadataServiceApiSettings>>().Value;
                client.BaseAddress = new Uri(options.ContentMetadataServiceUrl);
            });

            return services;
        }
    }

}
