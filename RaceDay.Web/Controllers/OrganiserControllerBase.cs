using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    /// <summary>Base class for every Organiser-only controller.</summary>
    [Authorize(Roles = RoleNames.Organiser)]
    public abstract class OrganiserControllerBase : AppController
    {
        protected readonly IApiClient Api;

        protected OrganiserControllerBase(IApiClient api)
        {
            Api = api;
        }

        /// <summary>
        /// Loads an event and confirms it belongs to the logged-in Organiser. (The API enforces this
        /// too - this check just lets us show a proper page instead of an API error.)
        /// </summary>
        protected async Task<(EventDto? Event, IActionResult? Failure)> LoadOwnedEventAsync(int eventId)
        {
            var result = await Api.GetAsync<EventDto>($"api/events/{eventId}");

            if (result.IsNotFound)
            {
                return (null, NotFound());
            }

            if (!result.Success || result.Data == null)
            {
                ShowError(result);
                return (null, RedirectToAction("Index", "OrganiserEvents"));
            }

            if (result.Data.OrganiserId != CurrentUserId)
            {
                return (null, Forbid());
            }

            return (result.Data, null);
        }
    }
}
