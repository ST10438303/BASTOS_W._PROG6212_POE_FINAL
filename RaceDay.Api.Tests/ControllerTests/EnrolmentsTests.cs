using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs.Categories;
using RaceDay.Api.DTOs.Enrolments;
using RaceDay.Api.DTOs.Events;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.TestHelpers;
using Xunit;

namespace RaceDay.Api.Tests.ControllerTests
{
    public class EnrolmentsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public EnrolmentsTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Creates an event with one category, owned by a fresh Organiser, and
        /// returns both the Organiser's authenticated client and the new category id.
        /// </summary>
        private async Task<(HttpClient organiser, int categoryId)> CreateEventWithCategoryAsync(int maxParticipants = 50)
        {
            var organiser = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Organiser);

            var eventResponse = await organiser.PostAsJsonAsync("/api/events", new EventCreateDto
            {
                EventName = "Enrolment Test Event",
                EventDate = DateTime.UtcNow.AddMonths(1),
                Location = "Test Venue",
                Distance = 10m,
                EventType = EventTypes.Run
            });
            var ev = await eventResponse.Content.ReadFromJsonAsync<EventResponseDto>();

            var categoryResponse = await organiser.PostAsJsonAsync($"/api/events/{ev!.EventId}/categories", new CategoryCreateDto
            {
                CategoryName = "10km",
                DistanceKm = 10m,
                MaxParticipants = maxParticipants,
                EntryFee = 100m
            });
            var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponseDto>();

            return (organiser, category!.CategoryId);
        }

        [Fact]
        public async Task Enrol_AsParticipant_ReturnsCreatedAndRecordsEnrolment()
        {
            var (_, categoryId) = await CreateEventWithCategoryAsync();
            var participant = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Participant);

            var response = await participant.PostAsJsonAsync("/api/enrolments", new EnrolmentCreateDto { CategoryId = categoryId });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<EnrolmentResponseDto>();
            Assert.NotNull(body);
            Assert.Equal(categoryId, body!.CategoryId);
            Assert.False(string.IsNullOrWhiteSpace(body.RaceNumber));

            // Confirm it shows up in the participant's own enrolment list.
            var mineResponse = await participant.GetAsync("/api/enrolments/mine");
            var mine = await mineResponse.Content.ReadFromJsonAsync<List<EnrolmentResponseDto>>();
            Assert.Contains(mine!, e => e.CategoryId == categoryId);
        }

        [Fact]
        public async Task Enrol_AsOrganiser_ReturnsForbidden()
        {
            // Verifies that Participant-only functionality is rejected for an Organiser.
            var (_, categoryId) = await CreateEventWithCategoryAsync();
            var anotherOrganiser = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Organiser);

            var response = await anotherOrganiser.PostAsJsonAsync("/api/enrolments", new EnrolmentCreateDto { CategoryId = categoryId });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Enrol_Twice_InSameCategory_ReturnsConflict()
        {
            var (_, categoryId) = await CreateEventWithCategoryAsync();
            var participant = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Participant);

            var first = await participant.PostAsJsonAsync("/api/enrolments", new EnrolmentCreateDto { CategoryId = categoryId });
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await participant.PostAsJsonAsync("/api/enrolments", new EnrolmentCreateDto { CategoryId = categoryId });
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task Enrol_WhenCategoryFull_ReturnsBadRequest()
        {
            var (_, categoryId) = await CreateEventWithCategoryAsync(maxParticipants: 1);

            var firstParticipant = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Participant);
            var firstResult = await firstParticipant.PostAsJsonAsync("/api/enrolments", new EnrolmentCreateDto { CategoryId = categoryId });
            Assert.Equal(HttpStatusCode.Created, firstResult.StatusCode);

            var secondParticipant = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Participant);
            var secondResult = await secondParticipant.PostAsJsonAsync("/api/enrolments", new EnrolmentCreateDto { CategoryId = categoryId });

            Assert.Equal(HttpStatusCode.BadRequest, secondResult.StatusCode);
        }

        [Fact]
        public async Task GetEventEnrolments_AsOwningOrganiser_ReturnsAllEnrolments()
        {
            var (organiser, categoryId) = await CreateEventWithCategoryAsync();
            var participant = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Participant);
            var enrolResponse = await participant.PostAsJsonAsync("/api/enrolments", new EnrolmentCreateDto { CategoryId = categoryId });
            var enrolment = await enrolResponse.Content.ReadFromJsonAsync<EnrolmentResponseDto>();
            var eventId = enrolment!.EventId;

            var response = await organiser.GetAsync($"/api/events/{eventId}/enrolments");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var enrolments = await response.Content.ReadFromJsonAsync<List<EnrolmentResponseDto>>();
            Assert.Single(enrolments!);
        }

        [Fact]
        public async Task GetEventEnrolments_AsParticipant_ReturnsForbidden()
        {
            var (_, categoryId) = await CreateEventWithCategoryAsync();
            var participant = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Participant);
            var enrolResponse = await participant.PostAsJsonAsync("/api/enrolments", new EnrolmentCreateDto { CategoryId = categoryId });
            var enrolment = await enrolResponse.Content.ReadFromJsonAsync<EnrolmentResponseDto>();

            var response = await participant.GetAsync($"/api/events/{enrolment!.EventId}/enrolments");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Cancel_ThenEnrolAgain_ReactivatesEnrolment()
        {
            var (_, categoryId) = await CreateEventWithCategoryAsync();
            var participant = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Participant);
            var first = await participant.PostAsJsonAsync("/api/enrolments", new EnrolmentCreateDto { CategoryId = categoryId });
            var enrolment = await first.Content.ReadFromJsonAsync<EnrolmentResponseDto>();

            var cancel = await participant.DeleteAsync($"/api/enrolments/{enrolment!.EnrolmentId}");
            Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

            var again = await participant.PostAsJsonAsync("/api/enrolments", new EnrolmentCreateDto { CategoryId = categoryId });
            Assert.Equal(HttpStatusCode.Created, again.StatusCode);
            var reactivated = await again.Content.ReadFromJsonAsync<EnrolmentResponseDto>();
            Assert.Equal(EnrolmentStatus.Pending, reactivated!.Status);
        }

        [Fact]
        public async Task DeleteEvent_WithEnrolments_ReturnsConflict()
        {
            var (organiser, categoryId) = await CreateEventWithCategoryAsync();
            var participant = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Participant);
            var enrolResponse = await participant.PostAsJsonAsync("/api/enrolments", new EnrolmentCreateDto { CategoryId = categoryId });
            var enrolment = await enrolResponse.Content.ReadFromJsonAsync<EnrolmentResponseDto>();

            var response = await organiser.DeleteAsync($"/api/events/{enrolment!.EventId}");

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
    }
}
