using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using Microsoft.Owin.Security.OAuth;
using Newtonsoft.Json.Serialization;
using Serilog;
using Services.Automapper;

namespace API
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            //Custom Global Exception Handler
            config.Services.Replace(typeof(IExceptionHandler), new Handlers.ExceptionsHandler());

            // Configure Web API to use only bearer token authentication.
            config.SuppressDefaultHostAuthentication();
            config.Filters.Add(new HostAuthenticationFilter(OAuthDefaults.AuthenticationType));

            //Custom Filter
            config.Filters.Add(new Filters.ModelStateValidation());
            config.Filters.Add(new Filters.ExceptionHandling());

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );


            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                //.WriteTo.Async(a => a.RollingFile(@"C:\Logs\{Date}.txt", retainedFileCountLimit: 10))
                .WriteTo.RollingFile(@"C:\Logs\{Date}.txt", retainedFileCountLimit: 10)
                //.WriteTo.File("log-.txt", rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }
    }
}
