using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    [AllowAnonymous]
    public class HomeController : AppController
    {
        private readonly IApiClient _api;

        public HomeController(IApiClient api)
        {
            _api = api;
        }

        /// <summary>Home page: browse and filter upcoming events (filtering is done by the API).</summary>
        public async Task<IActionResult> Index(string? search, string? eventType, string? when)
        {
            // Organisers have their own home: the dashboard.
            if (User.IsInRole(RoleNames.Organiser))
            {
                return RedirectToAction("Dashboard", "Organiser");
            }

            var model = new HomeIndexViewModel
            {
                Search = search?.Trim(),
                EventType = eventType,
                UpcomingOnly = !string.Equals(when, "all", StringComparison.OrdinalIgnoreCase)
            };

            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(model.Search)) query.Add("search=" + Uri.EscapeDataString(model.Search));
            if (!string.IsNullOrWhiteSpace(model.EventType)) query.Add("eventType=" + Uri.EscapeDataString(model.EventType));
            if (model.UpcomingOnly) query.Add("upcomingOnly=true");

            var url = "api/events" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);
            var result = await _api.GetAsync<List<EventDto>>(url);

            if (result.Success)
            {
                model.Events = result.Data ?? new List<EventDto>();
            }
            else
            {
                ShowError(result);
            }

            return View(model);
        }

        [Route("Home/Status")]
        public IActionResult Status(int code = 500)
        {
            Response.StatusCode = code;
            ViewData["Code"] = code;
            return View("Status");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            Response.StatusCode = StatusCodes.Status500InternalServerError;
            ViewData["RequestId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return View();
        }
    }
}
