using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs.Auth;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.TestHelpers;
using Xunit;

namespace RaceDay.Api.Tests.ControllerTests
{
    public class AuthTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AuthTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Register_WithValidParticipantDetails_ReturnsCreated()
        {
            var client = _factory.CreateClient();
            var dto = new RegisterDto
            {
                FullName = "Lindiwe Dlamini",
                Email = $"lindiwe.{Guid.NewGuid():N}@test.co.za",
                Password = "SecurePass123!",
                Role = RoleNames.Participant
            };

            var response = await client.PostAsJsonAsync("/api/auth/register", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            Assert.NotNull(body);
            Assert.Equal(RoleNames.Participant, body!.Role);
        }

        [Fact]
        public async Task Register_WithValidOrganiserDetails_ReturnsCreated()
        {
            var client = _factory.CreateClient();
            var dto = new RegisterDto
            {
                FullName = "Thandiwe Mokoena",
                Email = $"thandiwe.{Guid.NewGuid():N}@test.co.za",
                Password = "SecurePass123!",
                Role = RoleNames.Organiser
            };

            var response = await client.PostAsJsonAsync("/api/auth/register", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Register_WithInvalidRole_ReturnsBadRequest()
        {
            var client = _factory.CreateClient();
            var dto = new RegisterDto
            {
                FullName = "Confused User",
                Email = $"confused.{Guid.NewGuid():N}@test.co.za",
                Password = "SecurePass123!",
                Role = "Admin" // Not a recognised role.
            };

            var response = await client.PostAsJsonAsync("/api/auth/register", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_ReturnsConflict()
        {
            var client = _factory.CreateClient();
            var email = $"duplicate.{Guid.NewGuid():N}@test.co.za";
            var dto = new RegisterDto
            {
                FullName = "First User",
                Email = email,
                Password = "SecurePass123!",
                Role = RoleNames.Participant
            };

            var first = await client.PostAsJsonAsync("/api/auth/register", dto);
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await client.PostAsJsonAsync("/api/auth/register", dto);
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task Login_WithCorrectCredentials_ReturnsOkAndIssuesSessionCookie()
        {
            var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                HandleCookies = true
            });

            var email = $"login.{Guid.NewGuid():N}@test.co.za";
            await client.PostAsJsonAsync("/api/auth/register", new RegisterDto
            {
                FullName = "Login Test",
                Email = email,
                Password = "SecurePass123!",
                Role = RoleNames.Participant
            });

            var response = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
            {
                Email = email,
                Password = "SecurePass123!"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.Contains("Set-Cookie"));
        }

        [Fact]
        public async Task Login_WithIncorrectPassword_ReturnsUnauthorized()
        {
            var client = _factory.CreateClient();
            var email = $"wrongpass.{Guid.NewGuid():N}@test.co.za";
            await client.PostAsJsonAsync("/api/auth/register", new RegisterDto
            {
                FullName = "Wrong Password Test",
                Email = email,
                Password = "SecurePass123!",
                Role = RoleNames.Participant
            });

            var response = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
            {
                Email = email,
                Password = "ThisIsNotTheRightPassword"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
            {
                Email = "does.not.exist@test.co.za",
                Password = "SecurePass123!"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetMyProfile_WithoutLoggingIn_ReturnsUnauthorized()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/users/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
