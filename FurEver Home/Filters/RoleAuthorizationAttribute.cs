using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FurEver_Home.Services;

namespace FurEver_Home.Filters
{

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RoleAuthorizationAttribute : AuthorizeAttribute
    {
        private readonly string[] _allowedRoles;

        public string Feature { get; set; }

        public RoleAuthorizationAttribute(params string[] roles)
        {
            _allowedRoles = roles ?? new string[0];
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext == null) return false;

            // Allow anonymous if explicitly decorated
            var allowAnonymous = httpContext.Items["__AllowAnonymous__"];
            if (allowAnonymous is bool && (bool)allowAnonymous) return true;

            // Check login
            var session = httpContext.Session;
            if (session == null || session["UserId"] == null) return false;

            if (!int.TryParse(session["UserId"].ToString(), out int userId)) return false;

            // If no roles required, allow
            if (_allowedRoles == null || _allowedRoles.Length == 0) return true;

            // Try to resolve RoleService from DI, otherwise create one and dispose it
            RoleService rs = null;
            bool dispose = false;
            try
            {
                rs = DependencyResolver.Current.GetService<RoleService>();
                if (rs == null)
                {
                    rs = new RoleService();
                    dispose = true;
                }

                // HasAnyRole already does case-insensitive matching
                return rs.HasAnyRole(userId, _allowedRoles);
            }
            finally
            {
                if (dispose)
                {
                    rs?.Dispose();
                }
            }
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext == null) return;

            var session = filterContext.HttpContext.Session;
            if (session == null || session["UserId"] == null)
            {
                // Not logged in - redirect to login (preserve returnUrl)
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary(
                        new { controller = "Account", action = "Login", returnUrl = filterContext.HttpContext.Request.RawUrl }
                    )
                );
                return;
            }

            // Logged in but not authorized -> redirect to Admin/AccessDenied with context
            filterContext.Result = new RedirectToRouteResult(
                new System.Web.Routing.RouteValueDictionary(
                    new
                    {
                        controller = "Admin",
                        action = "AccessDenied",
                        feature = Feature,
                        requiredRoles = _allowedRoles != null && _allowedRoles.Length > 0 ? string.Join(", ", _allowedRoles) : null
                    }
                )
            );

            // Optionally set TempData message
            if (filterContext.Controller != null)
            {
                filterContext.Controller.TempData["Error"] = "You do not have permission to access this resource.";
            }
        }
    }

    /// <summary>
    /// Specific authorization attributes for common role combinations
    /// </summary>
    public class SuperAdminOnlyAttribute : RoleAuthorizationAttribute
    {
        public SuperAdminOnlyAttribute() : base("Super Admin") { }
    }

    public class AdminOrModeratorAttribute : RoleAuthorizationAttribute
    {
        public AdminOrModeratorAttribute() : base("Super Admin", "Moderator") { }
    }

    public class AllAdminRolesAttribute : RoleAuthorizationAttribute
    {
        public AllAdminRolesAttribute() : base("Super Admin", "Moderator", "Support") { }
    }
}