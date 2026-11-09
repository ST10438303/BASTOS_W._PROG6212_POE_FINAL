using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    /// <summary>Registration, login and logout - all performed by calling the Part 2 API.</summary>
    public class AccountController : AppController
    {
        private readonly IApiClient _api;

        public AccountController(IApiClient api)
        {
            _api = api;
        }

        [HttpGet, AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToHome(User.FindFirstValue(ClaimTypes.Role));
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _api.LoginAsync(model.Email, model.Password);
            if (!result.Success || result.Data == null)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "We could not log you in. Please try again.");
                model.Password = string.Empty;
                return View(model);
            }

            await StartSessionAsync(result.Data);

            Success($"Welcome back, {result.Data.FullName}!");

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToHome(result.Data.Role);
        }

        [HttpGet, AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToHome(User.FindFirstValue(ClaimTypes.Role));
            }

            return View(new RegisterViewModel());
        }

        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var register = await _api.PostAsync<AuthResponse>(
                "api/auth/register",
                new RegisterRequest(model.FullName.Trim(), model.Email.Trim(), model.Password, model.PhoneNumber?.Trim(), model.Role));

            if (!register.Success)
            {
                ModelState.AddModelError(string.Empty, register.Error ?? "We could not create your account. Please try again.");
                model.Password = string.Empty;
                model.ConfirmPassword = string.Empty;
                return View(model);
            }

            // Log the new user straight in so they can get started.
            var login = await _api.LoginAsync(model.Email.Trim(), model.Password);
            if (!login.Success || login.Data == null)
            {
                Success("Your account has been created. Please log in.");
                return RedirectToAction(nameof(Login));
            }

            await StartSessionAsync(login.Data);
            Success($"Welcome to RaceDay, {login.Data.FullName}!");
            return RedirectToHome(login.Data.Role);
        }

        [HttpPost, Authorize]
        public async Task<IActionResult> Logout()
        {
            await _api.LogoutAsync();
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            Success("You have been logged out.");
            return RedirectToAction(nameof(Login));
        }

        [HttpGet, AllowAnonymous]
        public IActionResult AccessDenied()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View();
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>Stores the user's state in the session and signs them in to the MVC site.</summary>
        private async Task StartSessionAsync(AuthResponse user)
        {
            // (The API session cookie was already saved into the session by ApiClient.LoginAsync.)
            HttpContext.Session.SetString(SessionKeys.UserId, user.UserId.ToString());
            HttpContext.Session.SetString(SessionKeys.FullName, user.FullName);
            HttpContext.Session.SetString(SessionKeys.Role, user.Role);

            // Profile picture is shown in the navigation bar; failure to load it is not fatal.
            var profile = await _api.GetAsync<UserProfile>("api/users/me");
            HttpContext.Session.SetString(SessionKeys.ProfilePictureUrl, profile.Data?.ProfilePictureUrl ?? string.Empty);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = false });
        }

        private IActionResult RedirectToHome(string? role) =>
            role == RoleNames.Organiser
                ? RedirectToAction("Dashboard", "Organiser")
                : RedirectToAction("Index", "Home");
    }
}
