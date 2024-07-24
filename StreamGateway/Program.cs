using APIGatewayMain.ServiceCollectionExtensions;
using EncryptionService;
using KeyServiceAPI;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.OpenApi.Models;
using StreamGateway.Services.Implementations;
using StreamGateway.Services.Interfaces;
using StreamGatewayContracts.IntegrationContracts;
using StreamGatewayContracts.IntegrationContracts.Image;
using StreamGatewayContracts.IntegrationContracts.Video;
using StreamGatewayControllers.Middlewares;
using StreamGatewayMain.ServiceCollectionExtensions;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Serialization;

//TODO: NOW!!!!!!
// 3. All TODO:!!!!!!! 

//TODO: Logs should be in components and response Messages should be in Controllers !!!!!!!!!!!!!!!
//(W sensie poprostu uniknij duplikatow, niech sie zazebiaja, a logi w komponentach maja pierwsenstwo)

//TODO: Będzie sporo zapytań o obrazki. Prawdopodobnie trzeba będzie stworzyć osobny ImageService, i linki zwracane przez APIGateway
//będą bezpośrednio adresowane do imageService'u 
//W komercyjnej wersji pewnie byśmy wykorzystali CDN (Content Delivery Network) (jest platne)

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCommonConfiguration(builder.Configuration);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10737418240; // 10 GB //TODO: Config
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10737418240; // 10 GB
});

// Add services to the container.
builder.Services.AddTransient<IKeyServiceClient, KeyServiceClient>();
builder.Services.AddTransient<IFileEncryptor, FileEncryptor>();
builder.Services.AddTransient<IFileUploader, FileUploader>();
builder.Services.AddTransient<IImageStreamContract, ImageStreamService>(); //TODO: Change service to Contract in name, move to extension methods
builder.Services.AddTransient<IImageUploadContract, ImageUploadService>();

builder.Services.AddTransient<IVideoStreamContract, VideoStreamService>();
builder.Services.AddTransient<IVideoUploadContract, VideoUploadService>();

builder.Services.AddTransient<IUriContract, UriService>();

builder.Services.AddContentMetadataServiceAPI();
builder.Services.AddKeyServiceClient();

//TODO: Use safe cors policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.WithOrigins("*")
                  .AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
     {
         options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
     });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "StreamGateway API", Version = "v1" });
    c.OperationFilter<FileUploadOperation>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "StreamGateway API v1");
    });

    app.UseCors("AllowAll");
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseMiddleware<LoggingMiddleware>();

app.MapControllers();

app.Run();

public class FileUploadOperation : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileUploadParams = context.MethodInfo.GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile));
        if (!fileUploadParams.Any()) return;

        //operation.Parameters.Clear();
        //operation.Parameters.Add(new OpenApiParameter
        //{
        //    Name = "contentId",
        //    In = ParameterLocation.Query,
        //    Required = true,
        //    Schema = new OpenApiSchema
        //    {
        //        Type = "string",
        //        Format = "uuid"
        //    }
        //});

        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema>
                        {
                            ["file"] = new OpenApiSchema
                            {
                                Type = "string",
                                Format = "binary"
                            }
                        },
                        Required = new HashSet<string> { "file" }
                    }
                }
            }
        };
    }
}