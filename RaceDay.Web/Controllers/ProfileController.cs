using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    /// <summary>Both roles: view and update personal information and upload a profile picture.</summary>
    [Authorize]
    public class ProfileController : AppController
    {
        private readonly IApiClient _api;

        public ProfileController(IApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            var result = await _api.GetAsync<UserProfile>("api/users/me");
            if (!result.Success || result.Data == null)
            {
                ShowError(result);
                return RedirectToHomeForRole();
            }

            return View(ToViewModel(result.Data));
        }

        [HttpPost]
        public async Task<IActionResult> Update(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await FillReadOnlyDetailsAsync(model);
                return View("Index", model);
            }

            var result = await _api.PutAsync<UserProfile>(
                "api/users/me",
                new UpdateProfileRequest(model.FullName.Trim(), model.PhoneNumber?.Trim()));

            if (!result.Success || result.Data == null)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "We could not save your changes.");
                await FillReadOnlyDetailsAsync(model);
                return View("Index", model);
            }

            HttpContext.Session.SetString(SessionKeys.FullName, result.Data.FullName);
            Success("Your profile has been updated.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UploadPicture(IFormFile? picture)
        {
            if (picture == null || picture.Length == 0)
            {
                Error("Please choose an image to upload.");
                return RedirectToAction(nameof(Index));
            }

            var result = await _api.PostFileAsync<UserProfile>("api/users/me/picture", picture);
            if (result.Success && result.Data != null)
            {
                HttpContext.Session.SetString(SessionKeys.ProfilePictureUrl, result.Data.ProfilePictureUrl ?? string.Empty);
                Success("Your profile picture has been updated.");
            }
            else
            {
                ShowError(result);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task FillReadOnlyDetailsAsync(ProfileViewModel model)
        {
            var result = await _api.GetAsync<UserProfile>("api/users/me");
            if (result.Success && result.Data != null)
            {
                model.Email = result.Data.Email;
                model.Role = result.Data.Role;
                model.ProfilePictureUrl = result.Data.ProfilePictureUrl;
                model.MemberSince = result.Data.DateCreated;
            }
        }

        private static ProfileViewModel ToViewModel(UserProfile p) => new()
        {
            FullName = p.FullName,
            PhoneNumber = p.PhoneNumber,
            Email = p.Email,
            Role = p.Role,
            ProfilePictureUrl = p.ProfilePictureUrl,
            MemberSince = p.DateCreated
        };

        private IActionResult RedirectToHomeForRole() =>
            User.IsInRole(RoleNames.Organiser)
                ? RedirectToAction("Dashboard", "Organiser")
                : RedirectToAction("Index", "Home");
    }
}
