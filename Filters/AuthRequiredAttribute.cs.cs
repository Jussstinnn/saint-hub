using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SaintHub.Filters
{
    public class AuthRequiredAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = context.HttpContext.Session.GetString("USER_AUTH");

            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new RedirectToActionResult(
                    "Login",
                    "Account",
                    null
                );
            }
        }
    }
}
