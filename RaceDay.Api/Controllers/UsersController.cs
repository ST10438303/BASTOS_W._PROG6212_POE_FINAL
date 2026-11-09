using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Users;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers
{
    /// <summary>
    /// Both Organisers and Participants can view and update their own profile.
    /// </summary>
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly RaceDayContext _context;
        private readonly IBlobStorageService _blobStorage;

        public UsersController(RaceDayContext context, IBlobStorageService blobStorage)
        {
            _context = context;
            _blobStorage = blobStorage;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>Retrieves the logged-in user's own profile.</summary>
        /// <response code="200">Returns the profile.</response>
        /// <response code="401">Not authenticated.</response>
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyProfile()
        {
            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null) return NotFound();

            return Ok(ToDto(user));
        }

        /// <summary>Updates the logged-in user's own profile (full name and phone number only).</summary>
        /// <response code="200">Profile updated.</response>
        /// <response code="400">Validation failed.</response>
        /// <response code="401">Not authenticated.</response>
        [HttpPut("me")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileDto dto)
        {
            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null) return NotFound();

            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;
            await _context.SaveChangesAsync();

            return Ok(ToDto(user));
        }

        /// <summary>Uploads (or replaces) the logged-in user's profile picture. The image is stored in Azure Blob Storage.</summary>
        /// <remarks>Send as multipart/form-data with a single file field named "file". JPG, PNG or WEBP, maximum 5 MB.</remarks>
        /// <response code="200">Picture stored. Returns the updated profile.</response>
        /// <response code="400">No file, wrong type, or file too large.</response>
        /// <response code="401">Not authenticated.</response>
        /// <response code="503">Image storage is not available.</response>
        [HttpPost("me/picture")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null) return NotFound();

            var (image, error) = await ImageUploadValidator.ValidateAsync(file);
            if (image == null)
            {
                return BadRequest(new { message = error });
            }

            try
            {
                await using var stream = file.OpenReadStream();
                user.ProfilePictureUrl = await _blobStorage.UploadImageAsync(stream, image.ContentType, "profiles", image.Extension);
            }
            catch (BlobStorageNotConfiguredException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Image storage is not available right now." });
            }

            await _context.SaveChangesAsync();
            return Ok(ToDto(user));
        }

        private static UserProfileDto ToDto(User user) => new()
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            ProfilePictureUrl = user.ProfilePictureUrl,
            DateCreated = user.DateCreated
        };
    }
}
