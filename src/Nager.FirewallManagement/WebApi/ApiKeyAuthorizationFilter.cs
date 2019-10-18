using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Configuration;

namespace Nager.FirewallManagement.WebApi
{
    public class ApiKeyAuthorizationFilter : IAuthorizationFilter
    {
        public bool AllowMultiple { get { return true; } }

        public async Task<HttpResponseMessage> ExecuteAuthorizationFilterAsync(HttpActionContext actionContext,
            CancellationToken cancellationToken,
            Func<Task<HttpResponseMessage>> continuation)
        {
            var apiKey = ConfigurationManager.AppSettings["ApiKey"];

            if (actionContext.Request.Headers.TryGetValues("Api-Key", out var apiKeyHeaders))
            {
                var requestApiKey = apiKeyHeaders.FirstOrDefault();
                if (string.IsNullOrEmpty(requestApiKey))
                {
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }

                if (requestApiKey.Equals(apiKey))
                {
                    return await continuation();
                }
            }

            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }
    }
}
