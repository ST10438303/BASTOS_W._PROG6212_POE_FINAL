using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Events;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers
{
    /// <summary>
    /// Organisers can create, update, and delete events. Both Organisers and
    /// Participants can view events - browsing the event list is how Participants
    /// discover events to enter.
    /// </summary>
    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayContext _context;
        private readonly IBlobStorageService _blobStorage;

        public EventsController(RaceDayContext context, IBlobStorageService blobStorage)
        {
            _context = context;
            _blobStorage = blobStorage;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>Lists events, optionally filtered. Public - no login required, so Participants can browse before registering.</summary>
        /// <param name="search">Optional text matched against the event name and location.</param>
        /// <param name="eventType">Optional discipline filter: Run, Walk or Cycle.</param>
        /// <param name="upcomingOnly">When true, only events dated today or later are returned.</param>
        /// <response code="200">Returns the list of events ordered by date.</response>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<EventResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search = null,
            [FromQuery] string? eventType = null,
            [FromQuery] bool upcomingOnly = false)
        {
            var query = _context.Events.Include(e => e.Organiser).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(e => e.EventName.Contains(term) || e.Location.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(eventType))
            {
                var canonical = EventTypes.All.FirstOrDefault(t => t.Equals(eventType.Trim(), StringComparison.OrdinalIgnoreCase));
                if (canonical == null)
                {
                    return BadRequest(new { message = "eventType must be 'Run', 'Walk', or 'Cycle'." });
                }
                query = query.Where(e => e.EventType == canonical);
            }

            if (upcomingOnly)
            {
                var today = DateTime.UtcNow.Date;
                query = query.Where(e => e.EventDate >= today);
            }

            // Materialize the entities first (ToListAsync), then map to DTOs in
            // memory - ToDto cannot be translated to SQL by the EF Core provider.
            var events = await query.OrderBy(e => e.EventDate).ToListAsync();

            return Ok(events.Select(ToDto));
        }

        /// <summary>Lists the events owned by the logged-in Organiser, with the number of enrolments for each. Used by the Organiser dashboard.</summary>
        /// <response code="200">Returns the Organiser's events ordered by date.</response>
        /// <response code="403">Logged in user is not an Organiser.</response>
        [HttpGet("mine")]
        [Authorize(Roles = RoleNames.Organiser)]
        [ProducesResponseType(typeof(IEnumerable<EventResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMine()
        {
            var events = await _context.Events
                .Include(e => e.Organiser)
                .Where(e => e.OrganiserId == CurrentUserId)
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            var eventIds = events.Select(e => e.EventId).ToList();

            // Fetch just the event ids of non-cancelled enrolments, then count in memory.
            var enrolledEventIds = await _context.Enrolments
                .Where(en => en.Status != EnrolmentStatus.Cancelled && eventIds.Contains(en.Category!.EventId))
                .Select(en => en.Category!.EventId)
                .ToListAsync();

            var counts = enrolledEventIds.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());

            return Ok(events.Select(e =>
            {
                var dto = ToDto(e);
                dto.EnrolmentCount = counts.TryGetValue(e.EventId, out var count) ? count : 0;
                return dto;
            }));
        }

        /// <summary>Retrieves a single event by id, including its categories.</summary>
        /// <response code="200">Returns the event.</response>
        /// <response code="404">Event not found.</response>
        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var ev = await _context.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventId == id);
            if (ev == null) return NotFound(new { message = "Event not found." });

            return Ok(ToDto(ev));
        }

        /// <summary>Creates a new event. Organiser role only.</summary>
        /// <response code="201">Event created.</response>
        /// <response code="400">Validation failed (e.g. invalid EventType).</response>
        /// <response code="401">Not authenticated.</response>
        /// <response code="403">Logged in user is not an Organiser.</response>
        [HttpPost]
        [Authorize(Roles = RoleNames.Organiser)]
        [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create([FromBody] EventCreateDto dto)
        {
            if (!EventTypes.IsValid(dto.EventType))
            {
                return BadRequest(new { message = "EventType must be 'Run', 'Walk', or 'Cycle'." });
            }

            var ev = new Event
            {
                OrganiserId = CurrentUserId,
                EventName = dto.EventName,
                Description = dto.Description,
                EventDate = dto.EventDate,
                Location = dto.Location,
                RouteInfo = dto.RouteInfo,
                Distance = dto.Distance,
                EventType = dto.EventType,
                DateCreated = DateTime.UtcNow
            };

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();
            await _context.Entry(ev).Reference(e => e.Organiser).LoadAsync();

            return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, ToDto(ev));
        }

        /// <summary>Updates an event owned by the logged-in Organiser.</summary>
        /// <response code="200">Event updated.</response>
        /// <response code="400">Validation failed.</response>
        /// <response code="403">Not the owner of this event.</response>
        /// <response code="404">Event not found.</response>
        [HttpPut("{id}")]
        [Authorize(Roles = RoleNames.Organiser)]
        [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] EventUpdateDto dto)
        {
            var ev = await _context.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventId == id);
            if (ev == null) return NotFound(new { message = "Event not found." });

            if (ev.OrganiserId != CurrentUserId)
            {
                return Forbid();
            }

            if (!EventTypes.IsValid(dto.EventType))
            {
                return BadRequest(new { message = "EventType must be 'Run', 'Walk', or 'Cycle'." });
            }

            ev.EventName = dto.EventName;
            ev.Description = dto.Description;
            ev.EventDate = dto.EventDate;
            ev.Location = dto.Location;
            ev.RouteInfo = dto.RouteInfo;
            ev.Distance = dto.Distance;
            ev.EventType = dto.EventType;

            await _context.SaveChangesAsync();
            return Ok(ToDto(ev));
        }

        /// <summary>Uploads (or replaces) the banner image of an event owned by the logged-in Organiser. The image is stored in Azure Blob Storage.</summary>
        /// <remarks>Send as multipart/form-data with a single file field named "file". JPG, PNG or WEBP, maximum 5 MB.</remarks>
        /// <response code="200">Banner stored. Returns the public image URL.</response>
        /// <response code="400">No file, wrong type, or file too large.</response>
        /// <response code="403">Not the owner of this event.</response>
        /// <response code="404">Event not found.</response>
        /// <response code="503">Image storage is not available.</response>
        [HttpPost("{id}/banner")]
        [Authorize(Roles = RoleNames.Organiser)]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> UploadBanner(int id, IFormFile file)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound(new { message = "Event not found." });

            if (ev.OrganiserId != CurrentUserId)
            {
                return Forbid();
            }

            var (image, error) = await ImageUploadValidator.ValidateAsync(file);
            if (image == null)
            {
                return BadRequest(new { message = error });
            }

            try
            {
                await using var stream = file.OpenReadStream();
                ev.BannerImageUrl = await _blobStorage.UploadImageAsync(stream, image.ContentType, "banners", image.Extension);
            }
            catch (BlobStorageNotConfiguredException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Image storage is not available right now." });
            }

            await _context.SaveChangesAsync();
            return Ok(new { bannerImageUrl = ev.BannerImageUrl });
        }

        /// <summary>Deletes an event owned by the logged-in Organiser.</summary>
        /// <response code="200">Event deleted.</response>
        /// <response code="403">Not the owner of this event.</response>
        /// <response code="404">Event not found.</response>
        /// <response code="409">Event has enrolments and cannot be deleted.</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = RoleNames.Organiser)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound(new { message = "Event not found." });

            if (ev.OrganiserId != CurrentUserId)
            {
                return Forbid();
            }

            // Enrolments restrict deletion of their category (and therefore the event),
            // so refuse politely instead of letting the database throw.
            var hasEnrolments = await _context.Enrolments.AnyAsync(e => e.Category!.EventId == id);
            if (hasEnrolments)
            {
                return Conflict(new { message = "This event has enrolments and cannot be deleted." });
            }

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Event deleted." });
        }

        private static EventResponseDto ToDto(Event e) => new()
        {
            EventId = e.EventId,
            OrganiserId = e.OrganiserId,
            OrganiserName = e.Organiser?.FullName ?? string.Empty,
            EventName = e.EventName,
            Description = e.Description,
            EventDate = e.EventDate,
            Location = e.Location,
            RouteInfo = e.RouteInfo,
            Distance = e.Distance,
            EventType = e.EventType,
            BannerImageUrl = e.BannerImageUrl,
            DateCreated = e.DateCreated
        };
    }
}
