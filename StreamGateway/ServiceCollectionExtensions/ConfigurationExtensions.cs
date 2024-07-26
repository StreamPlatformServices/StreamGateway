using APIGatewayCoreUtilities.CommonConfiguration.ConfigurationModels;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace APIGatewayMain.ServiceCollectionExtensions
{
    internal static class ConfigurationExtensions
    {
        public static IServiceCollection AddCommonConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            //TODO: Rate limit in stream gateway
            //services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
            services.Configure<KestrelSettings>(configuration.GetSection("KestrelSettings"));
            services.Configure<ContentMetadataServiceApiSettings>(configuration.GetSection("ComponentsSettings:ContentMetadataServiceApiSettings"));
            services.Configure<KeyServiceClientSettings>(configuration.GetSection("ComponentsSettings:KeyServiceClientSettings"));

            return services;
        }

        public static WebApplicationBuilder AddKestrelSettings(this WebApplicationBuilder builder, KestrelSettings kestrelSettings)
        {
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.Listen(System.Net.IPAddress.Parse(kestrelSettings.ListeningIPv4Address), kestrelSettings.PortNumber);

                if (kestrelSettings.UseTls)
                {
                    serverOptions.Listen(System.Net.IPAddress.Parse(kestrelSettings.ListeningIPv4Address), kestrelSettings.TlsPortNumber, listenOptions =>
                    {
                        listenOptions.UseHttps();
                    });
                }

                serverOptions.Limits.MaxRequestBodySize = kestrelSettings.MaxUploadSize;
            });

            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = kestrelSettings.MaxUploadSize; 
            });

            return builder;
        }
    }
}
