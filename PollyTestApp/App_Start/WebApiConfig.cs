using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using Newtonsoft.Json.Serialization;
using AppvNext.Throttlebird.Throttling;

namespace PollyTestApp
{
    public static class WebApiConfig
    {
        internal static readonly IThrottleStore ThrottleStore = new InMemoryThrottleStore();

        public static void Register(HttpConfiguration config)
        {
            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Implement our custom throttling handler to limit API method calls.
            // Specify the throttle store, max number of allowed requests within specified timespan,
            // and message displayed in the error response when exceeded.
            
            config.MessageHandlers.Add(new ThrottlingHandler(
                ThrottleStore,
                id => 3,
                TimeSpan.FromSeconds(5),
                "You have exceeded the maximum number of allowed calls. Please wait until after the cooldown period to try again."
            ));
        }
    }
}
