using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Enrolments;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    /// <summary>
    /// Participants enter events by enrolling into a category. Organisers can view
    /// all enrolments for the events they own.
    /// </summary>
    [ApiController]
    [Route("api")]
    [Authorize]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public EnrolmentsController(RaceDayContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>Enrols the logged-in Participant into a chosen category.</summary>
        /// <response code="201">Enrolment created with status Pending and a generated race number. The Organiser confirms it later.</response>
        /// <response code="400">Category is full, or does not exist.</response>
        /// <response code="403">Logged in user is not a Participant.</response>
        /// <response code="409">Already enrolled in this category.</response>
        [HttpPost("enrolments")]
        [Authorize(Roles = RoleNames.Participant)]
        [ProducesResponseType(typeof(EnrolmentResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Enrol([FromBody] EnrolmentCreateDto dto)
        {
            var category = await _context.Categories
                .Include(c => c.Event)
                .Include(c => c.Enrolments)
                .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId);

            if (category == null)
            {
                return BadRequest(new { message = "Category does not exist." });
            }

            var takenSpots = category.Enrolments.Count(e => e.Status != EnrolmentStatus.Cancelled);
            if (category.MaxParticipants > 0 && takenSpots >= category.MaxParticipants)
            {
                return BadRequest(new { message = "This category is full." });
            }

            // The (ParticipantId, CategoryId) pair is unique, so a previously cancelled
            // enrolment is reactivated rather than inserted again.
            var existing = await _context.Enrolments.FirstOrDefaultAsync(
                e => e.ParticipantId == CurrentUserId && e.CategoryId == dto.CategoryId);

            if (existing != null && existing.Status != EnrolmentStatus.Cancelled)
            {
                return Conflict(new { message = "You are already enrolled in this category." });
            }

            Enrolment enrolment;
            if (existing != null)
            {
                existing.Status = EnrolmentStatus.Pending;
                existing.EnrolmentDate = DateTime.UtcNow;
                enrolment = existing;
            }
            else
            {
                enrolment = new Enrolment
                {
                    ParticipantId = CurrentUserId,
                    CategoryId = dto.CategoryId,
                    EnrolmentDate = DateTime.UtcNow,
                    Status = EnrolmentStatus.Pending,
                    RaceNumber = GenerateRaceNumber(category.EventId)
                };
                _context.Enrolments.Add(enrolment);
            }

            await _context.SaveChangesAsync();

            var participant = await _context.Users.FindAsync(CurrentUserId);

            return CreatedAtAction(nameof(GetMine), null, ToDto(enrolment, participant!.FullName, category));
        }

        /// <summary>Lists the logged-in Participant's own enrolments.</summary>
        /// <response code="200">Returns the participant's enrolments.</response>
        /// <response code="403">Logged in user is not a Participant.</response>
        [HttpGet("enrolments/mine")]
        [Authorize(Roles = RoleNames.Participant)]
        [ProducesResponseType(typeof(IEnumerable<EnrolmentResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMine()
        {
            var enrolments = await _context.Enrolments
                .Include(e => e.Participant)
                .Include(e => e.Category).ThenInclude(c => c!.Event)
                .Where(e => e.ParticipantId == CurrentUserId)
                .ToListAsync();

            return Ok(enrolments.Select(e => ToDto(e, e.Participant!.FullName, e.Category!)));
        }

        /// <summary>Lists all enrolments for a specific event. Restricted to the Organiser who owns it.</summary>
        /// <response code="200">Returns all enrolments for the event.</response>
        /// <response code="403">Not the owner of this event.</response>
        /// <response code="404">Event not found.</response>
        [HttpGet("events/{eventId}/enrolments")]
        [Authorize(Roles = RoleNames.Organiser)]
        [ProducesResponseType(typeof(IEnumerable<EnrolmentResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetForEvent(int eventId)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound(new { message = "Event not found." });

            if (ev.OrganiserId != CurrentUserId)
            {
                return Forbid();
            }

            var enrolments = await _context.Enrolments
                .Include(e => e.Participant)
                .Include(e => e.Category).ThenInclude(c => c!.Event)
                .Where(e => e.Category!.EventId == eventId)
                .ToListAsync();

            return Ok(enrolments.Select(e => ToDto(e, e.Participant!.FullName, e.Category!)));
        }

        /// <summary>Confirms a Pending enrolment. Restricted to the Organiser who owns the event.</summary>
        /// <response code="200">Enrolment confirmed.</response>
        /// <response code="400">The enrolment has been cancelled and cannot be confirmed.</response>
        /// <response code="403">Not the Organiser of the event this enrolment belongs to.</response>
        /// <response code="404">Enrolment not found.</response>
        [HttpPut("enrolments/{id}/confirm")]
        [Authorize(Roles = RoleNames.Organiser)]
        [ProducesResponseType(typeof(EnrolmentResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Confirm(int id)
        {
            var enrolment = await _context.Enrolments
                .Include(e => e.Participant)
                .Include(e => e.Category).ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentId == id);

            if (enrolment == null) return NotFound(new { message = "Enrolment not found." });

            if (enrolment.Category!.Event!.OrganiserId != CurrentUserId)
            {
                return Forbid();
            }

            if (enrolment.Status == EnrolmentStatus.Cancelled)
            {
                return BadRequest(new { message = "A cancelled enrolment cannot be confirmed." });
            }

            enrolment.Status = EnrolmentStatus.Confirmed;
            await _context.SaveChangesAsync();

            return Ok(ToDto(enrolment, enrolment.Participant!.FullName, enrolment.Category));
        }

        /// <summary>Cancels the logged-in Participant's own enrolment.</summary>
        /// <response code="200">Enrolment cancelled.</response>
        /// <response code="403">Not the owner of this enrolment.</response>
        /// <response code="404">Enrolment not found.</response>
        [HttpDelete("enrolments/{id}")]
        [Authorize(Roles = RoleNames.Participant)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancel(int id)
        {
            var enrolment = await _context.Enrolments.FindAsync(id);
            if (enrolment == null) return NotFound(new { message = "Enrolment not found." });

            if (enrolment.ParticipantId != CurrentUserId)
            {
                return Forbid();
            }

            enrolment.Status = EnrolmentStatus.Cancelled;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Enrolment cancelled." });
        }

        private static string GenerateRaceNumber(int eventId) =>
            $"EVT{eventId}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

        private static EnrolmentResponseDto ToDto(Enrolment e, string participantName, Category category) => new()
        {
            EnrolmentId = e.EnrolmentId,
            ParticipantId = e.ParticipantId,
            ParticipantName = participantName,
            CategoryId = e.CategoryId,
            CategoryName = category.CategoryName,
            EventId = category.EventId,
            EventName = category.Event?.EventName ?? string.Empty,
            EnrolmentDate = e.EnrolmentDate,
            Status = e.Status,
            RaceNumber = e.RaceNumber
        };
    }
}
