using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace eSchool.Infrastructure
{
    public sealed class AdminOnlyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.Session.GetInt32("RoleId") != 1)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            if (context.HttpContext.Session.GetInt32("MustChangePassword") == 1)
            {
                context.Result = new RedirectToActionResult("ForceChangePassword", "Account", null);
            }
        }
    }
}
