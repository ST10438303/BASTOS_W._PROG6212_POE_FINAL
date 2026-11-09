using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    /// <summary>Organiser: add age or distance categories to an event.</summary>
    [Route("organiser/events/{eventId:int}/categories")]
    public class OrganiserCategoriesController : OrganiserControllerBase
    {
        public OrganiserCategoriesController(IApiClient api) : base(api) { }

        [HttpGet("")]
        public async Task<IActionResult> Index(int eventId)
        {
            var (ev, failure) = await LoadOwnedEventAsync(eventId);
            if (failure != null) return failure;

            return View(await BuildPageAsync(ev!, new CategoryFormViewModel { EventId = eventId, MaxParticipants = 0, EntryFee = 0 }));
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(int eventId, [Bind(Prefix = nameof(CategoriesPageViewModel.NewCategory))] CategoryFormViewModel model)
        {
            var (ev, failure) = await LoadOwnedEventAsync(eventId);
            if (failure != null) return failure;

            model.EventId = eventId;
            model.CategoryId = null;

            if (!ModelState.IsValid)
            {
                return View("Index", await BuildPageAsync(ev!, model));
            }

            var result = await Api.PostAsync<CategoryDto>($"api/events/{eventId}/categories", ToRequest(model));
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "We could not add the category.");
                return View("Index", await BuildPageAsync(ev!, model));
            }

            Success($"Category \"{model.CategoryName}\" added.");
            return RedirectToAction(nameof(Index), new { eventId });
        }

        [HttpGet("{id:int}/edit")]
        public async Task<IActionResult> Edit(int eventId, int id)
        {
            var (ev, failure) = await LoadOwnedEventAsync(eventId);
            if (failure != null) return failure;

            var category = await FindCategoryAsync(eventId, id);
            if (category == null) return NotFound();

            ViewData["EventName"] = ev!.EventName;
            return View(new CategoryFormViewModel
            {
                EventId = eventId,
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                DistanceKm = category.DistanceKm,
                MinAge = category.MinAge,
                MaxAge = category.MaxAge,
                MaxParticipants = category.MaxParticipants,
                EntryFee = category.EntryFee
            });
        }

        [HttpPost("{id:int}/edit")]
        public async Task<IActionResult> Edit(int eventId, int id, CategoryFormViewModel model)
        {
            var (ev, failure) = await LoadOwnedEventAsync(eventId);
            if (failure != null) return failure;

            model.EventId = eventId;
            model.CategoryId = id;
            ViewData["EventName"] = ev!.EventName;

            if (!ModelState.IsValid) return View(model);

            var result = await Api.PutAsync<CategoryDto>($"api/categories/{id}", ToRequest(model));
            if (!result.Success)
            {
                if (result.IsNotFound) return NotFound();

                ModelState.AddModelError(string.Empty, result.Error ?? "We could not save your changes.");
                return View(model);
            }

            Success("Category updated.");
            return RedirectToAction(nameof(Index), new { eventId });
        }

        [HttpPost("{id:int}/delete")]
        public async Task<IActionResult> Delete(int eventId, int id)
        {
            var (_, failure) = await LoadOwnedEventAsync(eventId);
            if (failure != null) return failure;

            var result = await Api.DeleteAsync($"api/categories/{id}");
            if (result.Success)
            {
                Success("Category deleted.");
            }
            else
            {
                ShowError(result);
            }

            return RedirectToAction(nameof(Index), new { eventId });
        }

        // ------------------------------------------------------------------ helpers

        private async Task<CategoriesPageViewModel> BuildPageAsync(EventDto ev, CategoryFormViewModel form)
        {
            var page = new CategoriesPageViewModel { Event = ev, NewCategory = form };

            var categories = await Api.GetAsync<List<CategoryDto>>($"api/events/{ev.EventId}/categories");
            if (categories.Success)
            {
                page.Categories = categories.Data ?? new List<CategoryDto>();
            }
            else
            {
                ShowError(categories);
            }

            return page;
        }

        private async Task<CategoryDto?> FindCategoryAsync(int eventId, int categoryId)
        {
            var categories = await Api.GetAsync<List<CategoryDto>>($"api/events/{eventId}/categories");
            return categories.Data?.FirstOrDefault(c => c.CategoryId == categoryId);
        }

        private static CategoryRequest ToRequest(CategoryFormViewModel m) => new(
            m.CategoryName.Trim(), m.DistanceKm, m.MinAge, m.MaxAge, m.MaxParticipants ?? 0, m.EntryFee ?? 0);
    }
}
