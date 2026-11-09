using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    /// <summary>Organiser: capture finish times and positions after an event. Saved results are visible to participants straight away.</summary>
    [Route("organiser/events/{eventId:int}/results")]
    public class OrganiserResultsController : OrganiserControllerBase
    {
        private static readonly string[] TimeFormats = { @"h\:mm\:ss", @"hh\:mm\:ss" };

        public OrganiserResultsController(IApiClient api) : base(api) { }

        [HttpGet("")]
        public async Task<IActionResult> Index(int eventId)
        {
            var (ev, failure) = await LoadOwnedEventAsync(eventId);
            if (failure != null) return failure;

            var model = new ResultsEntryPageViewModel { Event = ev! };

            var enrolments = await Api.GetAsync<List<EnrolmentDto>>($"api/events/{eventId}/enrolments");
            if (!enrolments.Success)
            {
                ShowError(enrolments);
                return View(model);
            }

            var results = await Api.GetAsync<List<ResultDto>>($"api/events/{eventId}/results");
            if (!results.Success)
            {
                ShowError(results);
            }

            var resultsByEnrolment = (results.Data ?? new List<ResultDto>())
                .ToDictionary(r => r.EnrolmentId);

            model.Rows = (enrolments.Data ?? new List<EnrolmentDto>())
                .Where(e => e.Status != "Cancelled")
                .OrderBy(e => e.CategoryName)
                .ThenBy(e => e.ParticipantName)
                .Select(e => new ResultRowViewModel
                {
                    Enrolment = e,
                    ExistingResult = resultsByEnrolment.TryGetValue(e.EnrolmentId, out var r) ? r : null
                })
                .ToList();

            return View(model);
        }

        [HttpPost("save")]
        public async Task<IActionResult> Save(int eventId, ResultFormViewModel form)
        {
            var (_, failure) = await LoadOwnedEventAsync(eventId);
            if (failure != null) return failure;

            if (!ModelState.IsValid)
            {
                Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
                      ?? "Please check the values you entered.");
                return RedirectToAction(nameof(Index), new { eventId });
            }

            TimeSpan? finishTime = null;
            if (!string.IsNullOrWhiteSpace(form.FinishTime))
            {
                if (!TimeSpan.TryParseExact(form.FinishTime.Trim(), TimeFormats, CultureInfo.InvariantCulture, out var parsed))
                {
                    Error("The finish time must look like 01:45:30 (hours:minutes:seconds).");
                    return RedirectToAction(nameof(Index), new { eventId });
                }

                finishTime = parsed;
            }
            else if (form.CompletionStatus == "Finished")
            {
                Error("Please enter a finish time (hh:mm:ss) for participants who finished.");
                return RedirectToAction(nameof(Index), new { eventId });
            }

            var request = new ResultRequest(form.EnrolmentId, finishTime, form.OverallPosition, form.CategoryPosition, form.CompletionStatus);

            var result = form.ResultId.HasValue
                ? await Api.PutAsync<ResultDto>($"api/results/{form.ResultId.Value}", request)
                : await Api.PostAsync<ResultDto>("api/results", request);

            if (result.Success && result.Data != null)
            {
                Success($"Result saved for {result.Data.ParticipantName}.");
            }
            else
            {
                ShowError(result);
            }

            return RedirectToAction(nameof(Index), new { eventId });
        }
    }
}
