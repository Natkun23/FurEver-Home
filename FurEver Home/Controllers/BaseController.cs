using FurEver_Home.Filters;
using FurEver_Home.Models;
using FurEver_Home.Services;
using System;
using System.Web.Mvc;

namespace FurEver_Home.Controllers
{
    public class BaseController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // ✅ PREVENT CACHING ON ALL PAGES (must come first, before any redirects)
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));
            Response.Cache.SetNoStore();
            Response.AppendHeader("Cache-Control", "no-cache, no-store, must-revalidate, private, max-age=0");
            Response.AppendHeader("Pragma", "no-cache");
            Response.AppendHeader("Expires", "0");

            // Skip authentication check if action allows anonymous access
            var allowAnonymous = filterContext.ActionDescriptor.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Length > 0
                              || filterContext.ActionDescriptor.ControllerDescriptor.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Length > 0;

            if (allowAnonymous)
            {
                base.OnActionExecuting(filterContext);
                return;
            }

            // Check if user is logged in
            if (Session["UserId"] == null)
            {
                // User is not logged in, redirect to login
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary(
                        new { controller = "Account", action = "Login" }
                    )
                );
                return;
            }

            // Set ViewBag data for back button prevention script
            SetNavigationViewBagData();

            base.OnActionExecuting(filterContext);
        }

        private void SetNavigationViewBagData()
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                using (var db = new FurEverHomeContext())
                using (var roleService = new RoleService(db))
                {
                    bool isAdmin = roleService.HasAnyRole(userId.Value, "Super Admin", "Moderator", "Support");
                    ViewBag.IsAdmin = isAdmin;
                    ViewBag.RedirectUrl = isAdmin
                        ? Url.Action("Dashboard", "Admin")
                        : Url.Action("Index", "Home"); // ✅ Changed from null to Home/Index
                }
            }
        }

        protected int? GetCurrentUserId()
        {
            if (Session == null || Session["UserId"] == null) return null;
            int id;
            return int.TryParse(Session["UserId"].ToString(), out id) ? (int?)id : null;
        }

        protected ActionResult EnsureUserHasAnyRole(RoleService roleService, params string[] requiredRoles)
        {
            if (roleService == null) throw new ArgumentNullException(nameof(roleService));
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            if (requiredRoles == null || requiredRoles.Length == 0)
            {
                return null;
            }

            if (!roleService.HasAnyRole(userId.Value, requiredRoles))
            {
                return RedirectToAction("AccessDenied", "Admin", new
                {
                    feature = (string)null,
                    requiredRoles = string.Join(", ", requiredRoles)
                });
            }

            return null;
        }

        protected ActionResult EnsureUserHasRole(RoleService roleService, string requiredRole)
        {
            return EnsureUserHasAnyRole(roleService, requiredRole);
        }
    }
}