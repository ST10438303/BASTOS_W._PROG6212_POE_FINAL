using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace RaceDay.Web.Services
{
    /// <summary>
    /// When an API call reports that the session is no longer valid, end the MVC session too and
    /// redirect to the login page with a friendly message instead of showing an error.
    /// </summary>
    public class ApiSessionExpiredFilter : IAsyncExceptionFilter
    {
        public async Task OnExceptionAsync(ExceptionContext context)
        {
            if (context.Exception is not ApiSessionExpiredException) return;

            var http = context.HttpContext;
            http.Session.Clear();
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            var tempData = http.RequestServices.GetRequiredService<ITempDataDictionaryFactory>().GetTempData(http);
            tempData["Error"] = "Your session has ended. Please log in again.";

            context.Result = new RedirectToActionResult("Login", "Account", null);
            context.ExceptionHandled = true;
        }
    }
}
