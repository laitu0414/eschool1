using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace eSchool.Infrastructure
{
    public sealed class RoleAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly HashSet<int> _allowedRoles;

        public RoleAuthorizeAttribute(params int[] allowedRoles)
        {
            _allowedRoles = allowedRoles.ToHashSet();
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var userId = session.GetInt32("UserId");
            var roleId = session.GetInt32("RoleId");

            Console.WriteLine($"[RoleAuthorize] Request Path: {context.HttpContext.Request.Path}");
            Console.WriteLine($"[RoleAuthorize] UserId: {userId?.ToString() ?? "NULL"}, RoleId: {roleId?.ToString() ?? "NULL"}");

            if (!userId.HasValue || !roleId.HasValue)
            {
                Console.WriteLine("[RoleAuthorize] Redirecting to Login because UserId or RoleId is null.");
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            if (session.GetInt32("MustChangePassword") == 1)
            {
                context.Result = new RedirectToActionResult("ForceChangePassword", "Account", null);
                return;
            }

            if (_allowedRoles.Contains(roleId.Value) || (_allowedRoles.Contains(1) && roleId.Value == 5))
            {
                return;
            }

            context.Result = roleId.Value switch
            {
                1 => new RedirectToActionResult("Index", "Admin", null),
                2 => new RedirectToActionResult("HoSoCaNhan", "GiaoVien", null),
                3 => new RedirectToActionResult("HoSoCaNhan", "HocSinh", null),
                4 => new RedirectToActionResult("HoSoCaNhan", "HocSinh", null),
                5 => new RedirectToActionResult("Index", "Admin", null),
                _ => new RedirectToActionResult("Login", "Account", null)
            };
        }
    }
}
