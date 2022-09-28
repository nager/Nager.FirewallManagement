using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Caching.Memory;
using Nager.FirewallManagement.Attributes;

namespace Nager.FirewallManagement.Middlewares
{
    public class ApiKeyMiddleware
    {
        private const string ApiKeyHeaderName = "X-Api-Key";

        private readonly RequestDelegate _next;
        private readonly IMemoryCache _memoryCache;
        private readonly int _maxFailures;


        public ApiKeyMiddleware(
            RequestDelegate next,
            IMemoryCache memoryCache)
        {
            this._next = next;
            this._memoryCache = memoryCache;
            this._maxFailures = 5;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var hasAuthorizeAttribute = context.Features.Get<IEndpointFeature>()?.Endpoint?.Metadata
                .Any(m => m is ApiKeyAttribute) ?? false;

            if (!hasAuthorizeAttribute)
            {
                await this._next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var receivedApiKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
            var expectedApiKey = configuration.GetValue<string>("ApiKey");

            if (!expectedApiKey.Equals(receivedApiKey))
            {
                var remoteIpAddress = context.Connection.RemoteIpAddress;
                if (remoteIpAddress != null)
                {
                    var cacheKey = $"bf-{remoteIpAddress}";
                    var cacheEntryOptions = new MemoryCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromMinutes(5)
                    };

                    if (!this._memoryCache.TryGetValue<int>(cacheKey, out var counter))
                    {
                        this._memoryCache.Set(cacheKey, 1, cacheEntryOptions);
                    }
                    else
                    {
                        this._memoryCache.Set(cacheKey, ++counter, cacheEntryOptions);
                    }

                    if (counter > this._maxFailures)
                    {
                        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                        return;
                    }
                }

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await this._next(context);
        }
    }
}
