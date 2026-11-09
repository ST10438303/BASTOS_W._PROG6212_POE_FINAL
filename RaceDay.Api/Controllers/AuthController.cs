using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Auth;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    /// <summary>
    /// Handles account registration and session-based (cookie) authentication.
    ///
    /// Design choice: the brief requires "session management to maintain the
    /// user's authenticated state and role for all subsequent requests" rather
    /// than a stateless bearer token. Cookie authentication was chosen because the
    /// authentication cookie IS the session: once a user logs in, the browser
    /// automatically attaches the cookie to every subsequent request (including
    /// calls made from the Swagger UI, which runs on the same origin as the API),
    /// and ASP.NET Core reconstructs the user's identity and role from it via
    /// [Authorize] / [Authorize(Roles = "...")] without the client managing a token.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthController(RaceDayContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        /// <summary>
        /// Registers a new user as either an Organiser or a Participant.
        /// </summary>
        /// <remarks>
        /// Request body: { fullName, email, password, phoneNumber, role }.
        /// Role must be exactly "Organiser" or "Participant" - this is how the
        /// system manages which of the two roles a new account is given.
        /// </remarks>
        /// <response code="201">Account created. Returns the new user's id and role.</response>
        /// <response code="400">Validation failed, or Role was not a recognised value.</response>
        /// <response code="409">A user with this email is already registered.</response>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (dto.Role != RoleNames.Organiser && dto.Role != RoleNames.Participant)
            {
                return BadRequest(new { message = $"Role must be either '{RoleNames.Organiser}' or '{RoleNames.Participant}'." });
            }

            var emailExists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (emailExists)
            {
                return Conflict(new { message = "A user with this email is already registered." });
            }

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Role = dto.Role,
                PhoneNumber = dto.PhoneNumber,
                DateCreated = DateTime.UtcNow
            };

            // Hash the password - the plain text value is never persisted.
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var response = new AuthResponseDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            };

            return CreatedAtAction(nameof(Register), new { id = user.UserId }, response);
        }

        /// <summary>
        /// Logs a user in and starts an authenticated session via a cookie.
        /// </summary>
        /// <remarks>
        /// Request body: { email, password }. On success, an authentication cookie
        /// is issued that carries the user's id and role for all subsequent requests.
        /// </remarks>
        /// <response code="200">Login successful. Session cookie issued.</response>
        /// <response code="401">Email or password is incorrect.</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Email or password is incorrect." });
            }

            var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (verification == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { message = "Email or password is incorrect." });
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            return Ok(new AuthResponseDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            });
        }

        /// <summary>Ends the current authenticated session.</summary>
        /// <response code="200">Logged out successfully.</response>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Logged out." });
        }
    }
}
