using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    /// <summary>Organiser: see everyone who has enrolled for an event, and confirm pending enrolments.</summary>
    [Route("organiser/events/{eventId:int}/enrolments")]
    public class OrganiserEnrolmentsController : OrganiserControllerBase
    {
        public OrganiserEnrolmentsController(IApiClient api) : base(api) { }

        [HttpGet("")]
        public async Task<IActionResult> Index(int eventId)
        {
            var (ev, failure) = await LoadOwnedEventAsync(eventId);
            if (failure != null) return failure;

            var model = new EnrolmentsPageViewModel { Event = ev! };

            var result = await Api.GetAsync<List<EnrolmentDto>>($"api/events/{eventId}/enrolments");
            if (result.Success)
            {
                model.Enrolments = (result.Data ?? new List<EnrolmentDto>())
                    .OrderBy(e => e.Status == "Cancelled")
                    .ThenBy(e => e.EnrolmentDate)
                    .ToList();
            }
            else
            {
                ShowError(result);
            }

            return View(model);
        }

        [HttpPost("{id:int}/confirm")]
        public async Task<IActionResult> Confirm(int eventId, int id)
        {
            var result = await Api.PutAsync<EnrolmentDto>($"api/enrolments/{id}/confirm");

            if (result.Success && result.Data != null)
            {
                Success($"{result.Data.ParticipantName}'s enrolment has been confirmed.");
            }
            else
            {
                ShowError(result);
            }

            return RedirectToAction(nameof(Index), new { eventId });
        }
    }
}
