using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Categories;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    /// <summary>
    /// Organisers define age- or distance-based categories for their own events.
    /// Both roles can view available categories for an event.
    /// </summary>
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public CategoriesController(RaceDayContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>Lists all categories for a specific event.</summary>
        /// <response code="200">Returns the categories.</response>
        /// <response code="404">Event not found.</response>
        [HttpGet("api/events/{eventId}/categories")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<CategoryResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetForEvent(int eventId)
        {
            var eventExists = await _context.Events.AnyAsync(e => e.EventId == eventId);
            if (!eventExists) return NotFound(new { message = "Event not found." });

            // Materialize first (ToListAsync), then map to DTOs in memory - ToDto
            // cannot be translated to SQL by the EF Core provider. Enrolments is
            // included so CurrentEnrolmentCount reflects real, current data.
            var categories = await _context.Categories
                .Include(c => c.Enrolments)
                .Where(c => c.EventId == eventId)
                .ToListAsync();

            return Ok(categories.Select(ToDto));
        }

        /// <summary>Adds a new category to an event owned by the logged-in Organiser.</summary>
        /// <response code="201">Category created.</response>
        /// <response code="403">Not the owner of this event.</response>
        /// <response code="404">Event not found.</response>
        [HttpPost("api/events/{eventId}/categories")]
        [Authorize(Roles = RoleNames.Organiser)]
        [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Create(int eventId, [FromBody] CategoryCreateDto dto)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound(new { message = "Event not found." });

            if (ev.OrganiserId != CurrentUserId)
            {
                return Forbid();
            }

            var category = new Category
            {
                EventId = eventId,
                CategoryName = dto.CategoryName,
                DistanceKm = dto.DistanceKm,
                MinAge = dto.MinAge,
                MaxAge = dto.MaxAge,
                MaxParticipants = dto.MaxParticipants,
                EntryFee = dto.EntryFee
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetForEvent), new { eventId }, ToDto(category));
        }

        /// <summary>Updates a category belonging to an event owned by the logged-in Organiser.</summary>
        /// <response code="200">Category updated.</response>
        /// <response code="403">Not the owner of this event.</response>
        /// <response code="404">Category not found.</response>
        [HttpPut("api/categories/{id}")]
        [Authorize(Roles = RoleNames.Organiser)]
        [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryCreateDto dto)
        {
            var category = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category == null) return NotFound(new { message = "Category not found." });

            if (category.Event!.OrganiserId != CurrentUserId)
            {
                return Forbid();
            }

            category.CategoryName = dto.CategoryName;
            category.DistanceKm = dto.DistanceKm;
            category.MinAge = dto.MinAge;
            category.MaxAge = dto.MaxAge;
            category.MaxParticipants = dto.MaxParticipants;
            category.EntryFee = dto.EntryFee;

            await _context.SaveChangesAsync();
            return Ok(ToDto(category));
        }

        /// <summary>Removes a category from an event owned by the logged-in Organiser.</summary>
        /// <response code="200">Category deleted.</response>
        /// <response code="403">Not the owner of this event.</response>
        /// <response code="404">Category not found.</response>
        /// <response code="409">Category has enrolments and cannot be deleted.</response>
        [HttpDelete("api/categories/{id}")]
        [Authorize(Roles = RoleNames.Organiser)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category == null) return NotFound(new { message = "Category not found." });

            if (category.Event!.OrganiserId != CurrentUserId)
            {
                return Forbid();
            }

            // Enrolments restrict deletion of their category, so refuse politely instead of
            // letting the database throw.
            var hasEnrolments = await _context.Enrolments.AnyAsync(e => e.CategoryId == id);
            if (hasEnrolments)
            {
                return Conflict(new { message = "This category has enrolments and cannot be deleted." });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Category deleted." });
        }

        private static CategoryResponseDto ToDto(Category c) => new()
        {
            CategoryId = c.CategoryId,
            EventId = c.EventId,
            CategoryName = c.CategoryName,
            DistanceKm = c.DistanceKm,
            MinAge = c.MinAge,
            MaxAge = c.MaxAge,
            MaxParticipants = c.MaxParticipants,
            EntryFee = c.EntryFee,
            CurrentEnrolmentCount = c.Enrolments?.Count(e => e.Status != EnrolmentStatus.Cancelled) ?? 0
        };
    }
}
