using Microsoft.AspNetCore.Http.Features;
using Nager.FirewallManagement.Attributes;

namespace Nager.FirewallManagement.Middlewares
{
    public class ApiKeyMiddleware
    {
        private const string ApiKeyHeaderName = "X-Api-Key";

        private readonly RequestDelegate _next;

        public ApiKeyMiddleware(RequestDelegate next)
        {
            this._next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var hasAuthorizeAttribute = context.Features.Get<IEndpointFeature>()?.Endpoint?.Metadata
                .Any(m => m is ApiKeyAttribute);

            if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var potentialApiKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
            var apiKey = configuration.GetValue<string>("ApiKey");

            if (!apiKey.Equals(potentialApiKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await this._next(context);
        }
    }
}
