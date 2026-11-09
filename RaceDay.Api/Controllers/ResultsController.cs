using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Results;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    /// <summary>
    /// Organisers capture finish times and positions for Participants after an
    /// event. Participants can view their own results.
    /// </summary>
    [ApiController]
    [Route("api")]
    [Authorize]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public ResultsController(RaceDayContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>Captures a finishing result for a participant's enrolment. Organiser role only.</summary>
        /// <response code="201">Result captured.</response>
        /// <response code="400">Validation failed (e.g. invalid CompletionStatus).</response>
        /// <response code="403">Not the Organiser of the event this enrolment belongs to.</response>
        /// <response code="404">Enrolment not found.</response>
        /// <response code="409">A result has already been captured for this enrolment.</response>
        [HttpPost("results")]
        [Authorize(Roles = RoleNames.Organiser)]
        [ProducesResponseType(typeof(ResultResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] ResultCreateDto dto)
        {
            if (!IsValidCompletionStatus(dto.CompletionStatus))
            {
                return BadRequest(new { message = "CompletionStatus must be 'Finished', 'DNF', or 'DQ'." });
            }

            var enrolment = await _context.Enrolments
                .Include(e => e.Participant)
                .Include(e => e.Category).ThenInclude(c => c!.Event)
                .Include(e => e.Result)
                .FirstOrDefaultAsync(e => e.EnrolmentId == dto.EnrolmentId);

            if (enrolment == null)
            {
                return NotFound(new { message = "Enrolment not found." });
            }

            if (enrolment.Category!.Event!.OrganiserId != CurrentUserId)
            {
                return Forbid();
            }

            if (enrolment.Result != null)
            {
                return Conflict(new { message = "A result has already been captured for this enrolment. Use PUT to correct it." });
            }

            var result = new Result
            {
                EnrolmentId = dto.EnrolmentId,
                CapturedByUserId = CurrentUserId,
                FinishTime = dto.FinishTime,
                OverallPosition = dto.OverallPosition,
                CategoryPosition = dto.CategoryPosition,
                CompletionStatus = dto.CompletionStatus
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            var totals = await GetFinisherCountsAsync(new[] { enrolment.Category!.EventId });
            return CreatedAtAction(nameof(GetMine), null, ToDto(result, enrolment, totals));
        }

        /// <summary>Retrieves the logged-in Participant's personal results history.</summary>
        /// <response code="200">Returns the participant's results.</response>
        /// <response code="403">Logged in user is not a Participant.</response>
        [HttpGet("results/mine")]
        [Authorize(Roles = RoleNames.Participant)]
        [ProducesResponseType(typeof(IEnumerable<ResultResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMine()
        {
            var results = await _context.Results
                .Include(r => r.Enrolment).ThenInclude(e => e!.Category).ThenInclude(c => c!.Event)
                .Where(r => r.Enrolment!.ParticipantId == CurrentUserId)
                .ToListAsync();

            var totals = await GetFinisherCountsAsync(results.Select(r => r.Enrolment!.Category!.EventId));
            return Ok(results.Select(r => ToDto(r, r.Enrolment!, totals)));
        }

        /// <summary>Retrieves all results for an event (public leaderboard view).</summary>
        /// <response code="200">Returns the event's results ordered by overall position.</response>
        /// <response code="404">Event not found.</response>
        [HttpGet("events/{eventId}/results")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<ResultResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetForEvent(int eventId)
        {
            var eventExists = await _context.Events.AnyAsync(e => e.EventId == eventId);
            if (!eventExists) return NotFound(new { message = "Event not found." });

            var results = await _context.Results
                .Include(r => r.Enrolment).ThenInclude(e => e!.Participant)
                .Include(r => r.Enrolment).ThenInclude(e => e!.Category).ThenInclude(c => c!.Event)
                .Where(r => r.Enrolment!.Category!.EventId == eventId)
                .OrderBy(r => r.OverallPosition)
                .ToListAsync();

            var totals = await GetFinisherCountsAsync(new[] { eventId });
            return Ok(results.Select(r => ToDto(r, r.Enrolment!, totals)));
        }

        /// <summary>Corrects a previously captured result. Organiser role only, must own the event.</summary>
        /// <response code="200">Result updated.</response>
        /// <response code="400">Validation failed.</response>
        /// <response code="403">Not the Organiser of the event this result belongs to.</response>
        /// <response code="404">Result not found.</response>
        [HttpPut("results/{id}")]
        [Authorize(Roles = RoleNames.Organiser)]
        [ProducesResponseType(typeof(ResultResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] ResultUpdateDto dto)
        {
            if (!IsValidCompletionStatus(dto.CompletionStatus))
            {
                return BadRequest(new { message = "CompletionStatus must be 'Finished', 'DNF', or 'DQ'." });
            }

            var result = await _context.Results
                .Include(r => r.Enrolment).ThenInclude(e => e!.Category).ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null) return NotFound(new { message = "Result not found." });

            if (result.Enrolment!.Category!.Event!.OrganiserId != CurrentUserId)
            {
                return Forbid();
            }

            result.FinishTime = dto.FinishTime;
            result.OverallPosition = dto.OverallPosition;
            result.CategoryPosition = dto.CategoryPosition;
            result.CompletionStatus = dto.CompletionStatus;

            await _context.SaveChangesAsync();

            var totals = await GetFinisherCountsAsync(new[] { result.Enrolment.Category!.EventId });
            return Ok(ToDto(result, result.Enrolment, totals));
        }

        private static bool IsValidCompletionStatus(string status) =>
            status is CompletionStatuses.Finished or CompletionStatuses.DNF or CompletionStatuses.DQ;

        /// <summary>
        /// Counts, per event, how many participants finished (CompletionStatus = Finished),
        /// so a result can be shown as e.g. "47th of 312 finishers". The ids are fetched
        /// from the database and grouped in memory to keep the query simple.
        /// </summary>
        private async Task<Dictionary<int, int>> GetFinisherCountsAsync(IEnumerable<int> eventIds)
        {
            var ids = eventIds.Distinct().ToList();

            var finishedEventIds = await _context.Results
                .Where(r => r.CompletionStatus == CompletionStatuses.Finished && ids.Contains(r.Enrolment!.Category!.EventId))
                .Select(r => r.Enrolment!.Category!.EventId)
                .ToListAsync();

            return finishedEventIds.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
        }

        private static ResultResponseDto ToDto(Result r, Enrolment enrolment, IReadOnlyDictionary<int, int> finisherCounts) => new()
        {
            ResultId = r.ResultId,
            EnrolmentId = r.EnrolmentId,
            ParticipantName = enrolment.Participant?.FullName ?? string.Empty,
            EventId = enrolment.Category?.EventId ?? 0,
            EventName = enrolment.Category?.Event?.EventName ?? string.Empty,
            EventDate = enrolment.Category?.Event?.EventDate ?? default,
            CategoryName = enrolment.Category?.CategoryName ?? string.Empty,
            FinishTime = r.FinishTime,
            OverallPosition = r.OverallPosition,
            CategoryPosition = r.CategoryPosition,
            CompletionStatus = r.CompletionStatus,
            TotalFinishers = finisherCounts.TryGetValue(enrolment.Category?.EventId ?? 0, out var total) ? total : 0
        };
    }
}
