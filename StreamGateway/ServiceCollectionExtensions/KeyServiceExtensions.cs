using APIGatewayCoreUtilities.CommonConfiguration.ConfigurationModels;
using KeyServiceAPI;
using Microsoft.Extensions.Options;

namespace StreamGatewayMain.ServiceCollectionExtensions
{

    public static class KeyServiceExtensions
    {
        public static IServiceCollection AddKeyServiceClient(this IServiceCollection services)
        {
            services.AddHttpClient<IKeyServiceClient, KeyServiceClient>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<KeyServiceClientSettings>>().Value;
                client.BaseAddress = new Uri(options.KeyServiceUrl);
            });

            return services;
        }
    }

}
