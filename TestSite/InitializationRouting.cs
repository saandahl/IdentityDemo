using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;

namespace TestSite
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class InitializationRouting : IInitializableModule
    {
        public static void Register(HttpConfiguration config)
        {
            // Attribute routing.
            config.MapHttpAttributeRoutes();

            RouteTable.Routes.MapRoute(
                name: "Identity",
                url: "Identity/{action}/{id}",
                defaults: new { controller = "Identity", action = "index", id = UrlParameter.Optional }
            );

            RouteTable.Routes.MapRoute(
                name: "Account",
                url: "Account/{action}/{id}",
                defaults: new { controller = "Account", action = "login", id = UrlParameter.Optional }
            );
        }

        public void Initialize(InitializationEngine context)
        {
            GlobalConfiguration.Configure(Register);
        }
        public void Preload(string[] parameters) { }
        public void Uninitialize(InitializationEngine context) { }
    }
}