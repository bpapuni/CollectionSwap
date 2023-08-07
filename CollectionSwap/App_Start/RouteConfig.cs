using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace CollectionSwap
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            //routes.MapRoute(
            //    name: "LoadPartial",
            //    url: "Manage/LoadPartial/{id}",
            //    defaults: new { controller = "Manage", action = "LoadPartial", id = UrlParameter.Optional }
            //);

            routes.MapRoute(
                name: "Account",
                url: "Manage/Account",
                defaults: new { controller = "Manage", action = "Index" }
            );

            routes.MapRoute(
                name: "ManageCollections",
                url: "Manage/ManageCollections",
                defaults: new { controller = "Manage", action = "Index" }
            );

            routes.MapRoute(
                name: "YourCollections",
                url: "Manage/YourCollections",
                defaults: new { controller = "Manage", action = "Index" }
            );

            routes.MapRoute(
                name: "SwapHistory",
                url: "Manage/SwapHistory",
                defaults: new { controller = "Manage", action = "Index" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
