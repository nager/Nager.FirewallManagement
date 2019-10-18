using log4net;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Nager.FirewallManagement.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Owin;
using Swashbuckle.Application;
using System;
using System.Collections.Generic;
using System.Net.Http.Formatting;
using System.Threading;
using System.Web.Http;
using Topshelf;

namespace Nager.FirewallManagement
{
    public class Controller : ServiceControl
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Controller));
        private const string ServiceName = "Nager FirewallManagement";
        private const string AppGuid = "aaba00d6-cd4a-4f0e-9b2b-68530b0ad7e6";
        private Mutex _mutex;

        private IDisposable _webApp;

        public bool Start(HostControl hostControl)
        {
            this._mutex = new Mutex(true, "Global\\" + AppGuid);
            if (!this._mutex.WaitOne(TimeSpan.Zero, true))
            {
                Log.Error($"{nameof(Start)} - Cannot start a second instance");
                return false;
            }

            Log.Debug($"{nameof(Start)} - {ServiceName}");

            var webApiPort = 8080;
            this.RegisterWebApi(webApiPort);

            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            Log.Debug($"{nameof(Stop)} - {ServiceName}");

            this._webApp?.Dispose();
            this._mutex.Dispose();

            return true;
        }

        private void RegisterWebApi(int port)
        {
            var url = $"http://*:{port}";
            var fullUrl = url.Replace("*", "localhost");

            Log.Info($"{nameof(RegisterWebApi)} - Swagger: {fullUrl}/swagger/");

            try
            {
                this._webApp = WebApp.Start(url, (app) =>
                {
                    //Use JSON friendly default settings
                    var defaultSettings = new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented,
                        ContractResolver = new CamelCasePropertyNamesContractResolver(),
                        Converters = new List<JsonConverter> { new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() }, }
                    };
                    JsonConvert.DefaultSettings = () => { return defaultSettings; };

                    //Access-Control-Allow-Origin
                    app.UseCors(CorsOptions.AllowAll);

                    var config = new HttpConfiguration();
                    config.Filters.Add(new ApiKeyAuthorizationFilter());

                    //Specify JSON as the default media type
                    config.Formatters.Clear();
                    config.Formatters.Add(new JsonMediaTypeFormatter());
                    config.Formatters.JsonFormatter.SerializerSettings = defaultSettings;

                    //Route all requests to the RootController by default
                    config.Routes.MapHttpRoute("api", "api/{controller}/{action}/{id}", defaults: new { id = RouteParameter.Optional });
                    config.MapHttpAttributeRoutes();

                    //Tell swagger to generate documentation based on the XML doc file output from msbuild
                    config.EnableSwagger(c =>
                    {
                        c.SingleApiVersion("1.0", ServiceName);
                        c.ApiKey("apiKey").Description("API Key Authentication").Name("Api-Key").In("header");
                    }).EnableSwaggerUi(c =>
                    {
                        c.EnableApiKeySupport("Api-Key", "header");
                    });

                    app.UseWebApi(config);
                });
            }
            catch (Exception exception)
            {
                Log.Error($"{nameof(RegisterWebApi)} - Execute first\r\nnetsh http delete urlacl http://*:{port}/\r\nnetsh http add urlacl http://*:{port}/ user=%username%", exception);
            }
        }
    }
}
