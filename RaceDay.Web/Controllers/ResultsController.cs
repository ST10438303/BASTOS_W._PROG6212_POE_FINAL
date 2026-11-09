using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    /// <summary>Participant: personal race history across all completed events.</summary>
    [Authorize(Roles = RoleNames.Participant)]
    public class ResultsController : AppController
    {
        private readonly IApiClient _api;

        public ResultsController(IApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            var result = await _api.GetAsync<List<ResultDto>>("api/results/mine");
            if (!result.Success)
            {
                ShowError(result);
                return View(new List<ResultDto>());
            }

            var results = (result.Data ?? new List<ResultDto>())
                .OrderByDescending(r => r.EventDate)
                .ToList();

            return View(results);
        }
    }
}
