using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs.Events;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.TestHelpers;
using Xunit;

namespace RaceDay.Api.Tests.ControllerTests
{
    public class EventsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public EventsTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static EventCreateDto SampleEvent() => new()
        {
            EventName = "Test Marathon",
            Description = "A test event created during unit testing.",
            EventDate = DateTime.UtcNow.AddMonths(2),
            Location = "Test City",
            RouteInfo = "Test route.",
            Distance = 42.2m,
            EventType = EventTypes.Run
        };

        [Fact]
        public async Task CreateEvent_AsOrganiser_ReturnsCreated()
        {
            var organiser = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Organiser);

            var response = await organiser.PostAsJsonAsync("/api/events", SampleEvent());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<EventResponseDto>();
            Assert.NotNull(body);
            Assert.Equal("Test Marathon", body!.EventName);
            Assert.Equal(EventTypes.Run, body.EventType);
        }

        [Fact]
        public async Task CreateEvent_AsParticipant_ReturnsForbidden()
        {
            // Verifies that Organiser-only functionality is rejected for a Participant.
            var participant = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Participant);

            var response = await participant.PostAsJsonAsync("/api/events", SampleEvent());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_WithoutAuthentication_ReturnsUnauthorized()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/events", SampleEvent());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_WithInvalidEventType_ReturnsBadRequest()
        {
            var organiser = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Organiser);
            var dto = SampleEvent();
            dto.EventType = "Swim"; // Not a supported discipline.

            var response = await organiser.PostAsJsonAsync("/api/events", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetAllEvents_WithoutAuthentication_ReturnsOk()
        {
            // Browsing events is public, so Participants can discover events before registering.
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/events");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateEvent_AsNonOwnerOrganiser_ReturnsForbidden()
        {
            var organiserA = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Organiser);
            var createResponse = await organiserA.PostAsJsonAsync("/api/events", SampleEvent());
            var created = await createResponse.Content.ReadFromJsonAsync<EventResponseDto>();

            var organiserB = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Organiser);
            var updateDto = SampleEvent();
            updateDto.EventName = "Hijacked Event Name";

            var response = await organiserB.PutAsJsonAsync($"/api/events/{created!.EventId}", updateDto);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteEvent_AsOwnerOrganiser_ReturnsOk()
        {
            var organiser = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Organiser);
            var createResponse = await organiser.PostAsJsonAsync("/api/events", SampleEvent());
            var created = await createResponse.Content.ReadFromJsonAsync<EventResponseDto>();

            var response = await organiser.DeleteAsync($"/api/events/{created!.EventId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
