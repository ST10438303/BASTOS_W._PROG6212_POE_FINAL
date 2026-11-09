using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    /// <summary>Organiser: create, edit and delete events (including the banner image).</summary>
    [Route("organiser/events")]
    public class OrganiserEventsController : OrganiserControllerBase
    {
        public OrganiserEventsController(IApiClient api) : base(api) { }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var result = await Api.GetAsync<List<EventDto>>("api/events/mine");
            if (!result.Success)
            {
                ShowError(result);
                return View(new List<EventDto>());
            }

            return View(result.Data ?? new List<EventDto>());
        }

        [HttpGet("create")]
        public IActionResult Create()
        {
            return View("Form", new EventFormViewModel { EventDate = DateTime.UtcNow.Date.AddMonths(1) });
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(EventFormViewModel model)
        {
            if (!ModelState.IsValid) return View("Form", model);

            var result = await Api.PostAsync<EventDto>("api/events", ToRequest(model));
            if (!result.Success || result.Data == null)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "We could not create the event.");
                return View("Form", model);
            }

            await UploadBannerIfProvidedAsync(result.Data.EventId, model.BannerFile);

            Success($"\"{result.Data.EventName}\" has been created. Add some categories so participants can enter.");
            return RedirectToAction("Index", "OrganiserCategories", new { eventId = result.Data.EventId });
        }

        [HttpGet("{id:int}/edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var (ev, failure) = await LoadOwnedEventAsync(id);
            if (failure != null) return failure;

            return View("Form", new EventFormViewModel
            {
                EventId = ev!.EventId,
                EventName = ev.EventName,
                Description = ev.Description,
                EventDate = ev.EventDate.Date,
                Location = ev.Location,
                RouteInfo = ev.RouteInfo,
                Distance = ev.Distance,
                EventType = ev.EventType,
                BannerImageUrl = ev.BannerImageUrl
            });
        }

        [HttpPost("{id:int}/edit")]
        public async Task<IActionResult> Edit(int id, EventFormViewModel model)
        {
            model.EventId = id;

            if (!ModelState.IsValid)
            {
                await KeepCurrentBannerAsync(model);
                return View("Form", model);
            }

            var result = await Api.PutAsync<EventDto>($"api/events/{id}", ToRequest(model));
            if (!result.Success || result.Data == null)
            {
                if (result.IsNotFound) return NotFound();
                if (result.IsForbidden) return Forbid();

                ModelState.AddModelError(string.Empty, result.Error ?? "We could not save your changes.");
                await KeepCurrentBannerAsync(model);
                return View("Form", model);
            }

            await UploadBannerIfProvidedAsync(id, model.BannerFile);

            Success("Event updated.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("{id:int}/delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await Api.DeleteAsync($"api/events/{id}");

            if (result.Success)
            {
                Success("Event deleted.");
            }
            else
            {
                ShowError(result);
            }

            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------------------ helpers

        private static EventRequest ToRequest(EventFormViewModel m) => new(
            m.EventName.Trim(),
            m.Description?.Trim(),
            m.EventDate!.Value,
            m.Location.Trim(),
            m.RouteInfo?.Trim(),
            m.Distance!.Value,
            m.EventType);

        /// <summary>The banner goes to the API (which stores it in Azure Blob Storage).</summary>
        private async Task UploadBannerIfProvidedAsync(int eventId, IFormFile? file)
        {
            if (file == null || file.Length == 0) return;

            var upload = await Api.PostFileAsync<BannerUploadResponse>($"api/events/{eventId}/banner", file);
            if (!upload.Success)
            {
                Warning("The event was saved, but the banner image could not be uploaded: " +
                        (upload.Error ?? "please try again.") + " You can add it again by editing the event.");
            }
        }

        private async Task KeepCurrentBannerAsync(EventFormViewModel model)
        {
            if (!model.EventId.HasValue) return;

            var current = await Api.GetAsync<EventDto>($"api/events/{model.EventId.Value}");
            if (current.Success && current.Data != null)
            {
                model.BannerImageUrl = current.Data.BannerImageUrl;
            }
        }
    }
}
