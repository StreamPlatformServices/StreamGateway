using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace StreamGatewayControllers.Middlewares
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LoggingMiddleware> _logger;

        public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.OnCompleted(() =>
            {
                _logger.LogInformation("Response completed for request: {Path}", context.Request.Path);
                return Task.CompletedTask;
            });

            await _next(context);
        }
    }

}
