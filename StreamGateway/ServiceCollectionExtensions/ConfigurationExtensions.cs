using APIGatewayCoreUtilities.CommonConfiguration.ConfigurationModels;

namespace APIGatewayMain.ServiceCollectionExtensions
{
    internal static class ConfigurationExtensions
    {
        public static IServiceCollection AddCommonConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            //TODO: Rate limit in stream gateway
            //services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));

            services.Configure<ContentMetadataServiceApiSettings>(configuration.GetSection("ComponentsSettings:ContentMetadataServiceApiSettings"));

            return services;
        }
    }
}
