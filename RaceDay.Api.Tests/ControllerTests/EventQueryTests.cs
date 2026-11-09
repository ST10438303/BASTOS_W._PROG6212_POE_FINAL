using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs.Events;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.TestHelpers;
using Xunit;

namespace RaceDay.Api.Tests.ControllerTests
{
    /// <summary>Covers event filtering and the Organiser's "my events" dashboard data.</summary>
    public class EventQueryTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public EventQueryTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetEvents_FilteredByType_ReturnsOnlyThatType()
        {
            var organiser = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Organiser);
            var marker = Guid.NewGuid().ToString("N")[..8];
            await TestDataHelper.CreateEventAsync(organiser, $"Run {marker}", EventTypes.Run);
            await TestDataHelper.CreateEventAsync(organiser, $"Ride {marker}", EventTypes.Cycle);

            var client = _factory.CreateClient();
            var events = await client.GetFromJsonAsync<List<EventResponseDto>>($"/api/events?eventType=Cycle&search={marker}");

            Assert.Single(events!);
            Assert.Equal(EventTypes.Cycle, events![0].EventType);
        }

        [Fact]
        public async Task GetEvents_WithSearchText_MatchesNameOrLocation()
        {
            var organiser = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Organiser);
            var marker = Guid.NewGuid().ToString("N")[..8];
            await TestDataHelper.CreateEventAsync(organiser, "Findable Event", location: $"Place {marker}");
            await TestDataHelper.CreateEventAsync(organiser, "Another Event", location: "Elsewhere");

            var client = _factory.CreateClient();
            var events = await client.GetFromJsonAsync<List<EventResponseDto>>($"/api/events?search={marker}");

            Assert.Single(events!);
            Assert.Equal("Findable Event", events![0].EventName);
        }

        [Fact]
        public async Task GetEvents_UpcomingOnly_ExcludesPastEvents()
        {
            var organiser = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Organiser);
            var marker = Guid.NewGuid().ToString("N")[..8];
            await TestDataHelper.CreateEventAsync(organiser, $"Past {marker}", date: DateTime.UtcNow.AddMonths(-2));
            await TestDataHelper.CreateEventAsync(organiser, $"Future {marker}", date: DateTime.UtcNow.AddMonths(2));

            var client = _factory.CreateClient();
            var events = await client.GetFromJsonAsync<List<EventResponseDto>>($"/api/events?upcomingOnly=true&search={marker}");

            Assert.Single(events!);
            Assert.StartsWith("Future", events![0].EventName);
        }

        [Fact]
        public async Task GetEvents_WithInvalidType_ReturnsBadRequest()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/events?eventType=Swim");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetMyEvents_AsOrganiser_ReturnsOnlyOwnEventsWithEnrolmentCounts()
        {
            var organiser = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Organiser);
            var ev = await TestDataHelper.CreateEventAsync(organiser, "My Counted Event");
            var category = await TestDataHelper.CreateCategoryAsync(organiser, ev.EventId);

            var p1 = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Participant);
            var p2 = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Participant);
            await TestDataHelper.EnrolAsync(p1, category.CategoryId);
            var second = await TestDataHelper.EnrolAsync(p2, category.CategoryId);
            await p2.DeleteAsync($"/api/enrolments/{second.EnrolmentId}"); // cancelled enrolments are not counted

            var otherOrganiser = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Organiser);
            await TestDataHelper.CreateEventAsync(otherOrganiser, "Someone Else's Event");

            var mine = await organiser.GetFromJsonAsync<List<EventResponseDto>>("/api/events/mine");

            Assert.Single(mine!);
            Assert.Equal("My Counted Event", mine![0].EventName);
            Assert.Equal(1, mine[0].EnrolmentCount);
        }

        [Fact]
        public async Task GetMyEvents_AsParticipant_ReturnsForbidden()
        {
            var participant = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Participant);

            var response = await participant.GetAsync("/api/events/mine");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
