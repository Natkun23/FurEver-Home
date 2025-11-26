using System;
using System.Web.Mvc;
using FurEver_Home.Filters;

namespace FurEver_Home.Controllers
{
    public class BaseController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Check if user is logged in
            if (Session["UserId"] == null)
            {
                // User is not logged in, redirect to login
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary(
                        new { controller = "Account", action = "Login" }
                    )
                );
                return; // Important: return immediately after redirect
            }

            // ✅ ENHANCED: Aggressive cache prevention for all authenticated pages
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));
            Response.Cache.SetNoStore();

            // Additional headers to prevent all types of caching
            Response.AppendHeader("Cache-Control", "no-cache, no-store, must-revalidate, private");
            Response.AppendHeader("Pragma", "no-cache");
            Response.AppendHeader("Expires", "0");

            base.OnActionExecuting(filterContext);
        }
    }
}