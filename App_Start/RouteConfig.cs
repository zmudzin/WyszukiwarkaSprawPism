using System.Web.Mvc;
using System.Web.Routing;

public class RouteConfig
{
    public static void RegisterRoutes(RouteCollection routes)
    {
        routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

        routes.MapRoute(
            name: "Pisma",
            url: "pisma",
            defaults: new { controller = "Home", action = "Pisma" }
        );

        routes.MapRoute(
            name: "Sprawy",
            url: "sprawy",
            defaults: new { controller = "Home", action = "Sprawy" }
        );

        routes.MapRoute(
            name: "Default",
            url: "{controller}/{action}/{id}",
            defaults: new { controller = "Home", action = "Sprawy", id = UrlParameter.Optional }
        );
    }
}