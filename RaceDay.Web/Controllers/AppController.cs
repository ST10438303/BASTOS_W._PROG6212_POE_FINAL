using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    /// <summary>Shared helpers for all controllers: flash messages and current-user info.</summary>
    public abstract class AppController : Controller
    {
        protected int CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        protected void Success(string message) => TempData["Success"] = message;
        protected void Warning(string message) => TempData["Warning"] = message;
        protected void Error(string message) => TempData["Error"] = message;

        /// <summary>Shows an API failure to the user as a flash message.</summary>
        protected void ShowError<T>(ApiResult<T> result) =>
            Error(result.Error ?? "Something went wrong. Please try again.");

        /// <summary>Only allow redirects back to pages on this site.</summary>
        protected IActionResult RedirectToLocal(string? returnUrl, string fallbackAction, string fallbackController)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(fallbackAction, fallbackController);
        }
    }
}
