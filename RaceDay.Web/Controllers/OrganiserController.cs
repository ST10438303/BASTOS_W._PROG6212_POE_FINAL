using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    /// <summary>Organiser dashboard: events created, enrolments per event and upcoming dates.</summary>
    [Route("organiser")]
    public class OrganiserController : OrganiserControllerBase
    {
        public OrganiserController(IApiClient api) : base(api) { }

        [HttpGet("")]
        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var model = new DashboardViewModel();

            var result = await Api.GetAsync<List<EventDto>>("api/events/mine");
            if (result.Success)
            {
                model.Events = result.Data ?? new List<EventDto>();
                var today = DateTime.UtcNow.Date;
                model.Upcoming = model.Events
                    .Where(e => e.EventDate.Date >= today)
                    .OrderBy(e => e.EventDate)
                    .ToList();
            }
            else
            {
                ShowError(result);
            }

            return View(model);
        }
    }
}
