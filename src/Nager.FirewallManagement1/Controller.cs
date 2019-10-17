using log4net;
using Microsoft.Owin.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Owin;
using SimpleInjector;
using SimpleInjector.Integration.WebApi;
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
        private const string ServiceName = "Nager.FirewallManagement";
        private const string AppGuid = "0e7cae7e-3ec6-428d-b3d6-c46377ce2ca8";
        private Mutex _mutex;

        private Container _container;
        private IDisposable _webApp;

        public bool Start(HostControl hostControl)
        {
            this._mutex = new Mutex(true, "Global\\" + AppGuid);
            if (!this._mutex.WaitOne(TimeSpan.Zero, true))
            {
                Log.Error($"{nameof(Start)} - Cannot start a second instance");
                return false;
            }

            this._container = new Container();
            this._container.Verify();

            Log.Debug($"{nameof(Start)} - {ServiceName}");

            var webApiPort = 8080;
            this.RegisterWebApi(webApiPort);

            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            Log.Debug($"{nameof(Stop)} - {ServiceName}");

            //this._webApp?.Dispose();
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
                    var config = new HttpConfiguration();
                    config.DependencyResolver = new SimpleInjectorWebApiDependencyResolver(this._container);

                    //Use JSON friendly default settings
                    var defaultSettings = new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented,
                        ContractResolver = new CamelCasePropertyNamesContractResolver(),
                        Converters = new List<JsonConverter> { new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() }, }
                    };
                    JsonConvert.DefaultSettings = () => { return defaultSettings; };

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
                    }).EnableSwaggerUi();

                    app.UseWebApi(config);
                });
            }
            catch (Exception exception)
            {
                Log.Error($"{nameof(RegisterWebApi)} - run first AllowWebserver.cmd", exception);
            }
        }
    }
}
