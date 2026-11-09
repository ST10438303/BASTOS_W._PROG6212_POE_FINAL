using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    /// <summary>Participant: enrol in an event category and review/cancel their own enrolments.</summary>
    [Authorize(Roles = RoleNames.Participant)]
    public class EnrolmentsController : AppController
    {
        private readonly IApiClient _api;

        public EnrolmentsController(IApiClient api)
        {
            _api = api;
        }

        /// <summary>"My enrolments" - every event the Participant has entered and its status.</summary>
        public async Task<IActionResult> Index()
        {
            var result = await _api.GetAsync<List<EnrolmentDto>>("api/enrolments/mine");
            if (!result.Success)
            {
                ShowError(result);
                return View(new List<EnrolmentDto>());
            }

            var enrolments = (result.Data ?? new List<EnrolmentDto>())
                .OrderByDescending(e => e.EnrolmentDate)
                .ToList();

            return View(enrolments);
        }

        [HttpPost]
        public async Task<IActionResult> Create(int categoryId, int eventId)
        {
            var result = await _api.PostAsync<EnrolmentDto>("api/enrolments", new EnrolmentRequest(categoryId));

            if (result.Success && result.Data != null)
            {
                Success($"You are enrolled in {result.Data.EventName} ({result.Data.CategoryName}). " +
                        $"Your race number is {result.Data.RaceNumber}. The organiser will confirm your entry shortly.");
            }
            else
            {
                ShowError(result);
            }

            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id, int? eventId)
        {
            var result = await _api.DeleteAsync($"api/enrolments/{id}");

            if (result.Success)
            {
                Success("Your enrolment has been cancelled.");
            }
            else
            {
                ShowError(result);
            }

            return eventId.HasValue
                ? RedirectToAction("Details", "Events", new { id = eventId.Value })
                : RedirectToAction(nameof(Index));
        }
    }
}
