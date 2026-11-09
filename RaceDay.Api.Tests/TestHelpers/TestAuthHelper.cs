using System.Net.Http.Json;
using RaceDay.Api.DTOs.Auth;

namespace RaceDay.Api.Tests.TestHelpers
{
    /// <summary>
    /// Helpers that drive the real /api/auth endpoints to register and log in
    /// test users, returning an HttpClient whose cookie jar now carries an
    /// authenticated session - exactly how a real client would behave.
    /// </summary>
    public static class TestAuthHelper
    {
        public static async Task<HttpClient> RegisterAndLoginAsync(
            CustomWebApplicationFactory factory,
            string role,
            string? emailOverride = null)
        {
            var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                HandleCookies = true // Persist the auth cookie across requests on this client.
            });

            var email = emailOverride ?? $"{role.ToLower()}.{Guid.NewGuid():N}@test.raceday.co.za";

            var registerDto = new RegisterDto
            {
                FullName = $"Test {role}",
                Email = email,
                Password = "TestPass123!",
                Role = role
            };

            var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerDto);
            registerResponse.EnsureSuccessStatusCode();

            var loginDto = new LoginDto { Email = email, Password = "TestPass123!" };
            var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginDto);
            loginResponse.EnsureSuccessStatusCode();

            return client;
        }
    }
}
