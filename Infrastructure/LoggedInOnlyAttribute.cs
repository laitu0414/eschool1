using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace eSchool.Infrastructure
{
    public sealed class LoggedInOnlyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.HttpContext.Session.GetInt32("UserId").HasValue)
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
