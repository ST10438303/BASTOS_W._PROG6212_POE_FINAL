using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    /// <summary>Public event details page (banner, full information, categories and enrolment).</summary>
    [AllowAnonymous]
    public class EventsController : AppController
    {
        private readonly IApiClient _api;

        public EventsController(IApiClient api)
        {
            _api = api;
        }

        [HttpGet("Events/Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var eventResult = await _api.GetAsync<EventDto>($"api/events/{id}");
            if (eventResult.IsNotFound || eventResult.Data == null && eventResult.Success)
            {
                return NotFound();
            }

            if (!eventResult.Success)
            {
                ShowError(eventResult);
                return RedirectToAction("Index", "Home");
            }

            var model = new EventDetailsViewModel { Event = eventResult.Data! };

            var categories = await _api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories");
            if (categories.Success)
            {
                model.Categories = categories.Data ?? new List<CategoryDto>();
            }
            else
            {
                ShowError(categories);
            }

            // A logged-in Participant sees which categories they have already entered.
            if (User.IsInRole(RoleNames.Participant))
            {
                var mine = await _api.GetAsync<List<EnrolmentDto>>("api/enrolments/mine");
                if (mine.Success && mine.Data != null)
                {
                    model.MyEnrolments = mine.Data
                        .Where(e => e.EventId == id && e.Status != "Cancelled")
                        .ToDictionary(e => e.CategoryId);
                }
            }

            return View(model);
        }
    }
}
