using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs.Categories;
using RaceDay.Api.DTOs.Enrolments;
using RaceDay.Api.DTOs.Events;
using RaceDay.Api.DTOs.Results;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.TestHelpers;
using Xunit;

namespace RaceDay.Api.Tests.ControllerTests
{
    public class ResultsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ResultsTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private async Task<(HttpClient organiser, HttpClient participant, int enrolmentId)> SetUpEnrolledParticipantAsync()
        {
            var organiser = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Organiser);

            var eventResponse = await organiser.PostAsJsonAsync("/api/events", new EventCreateDto
            {
                EventName = "Results Test Event",
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
                MaxParticipants = 50,
                EntryFee = 100m
            });
            var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponseDto>();

            var participant = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Participant);
            var enrolResponse = await participant.PostAsJsonAsync("/api/enrolments", new EnrolmentCreateDto { CategoryId = category!.CategoryId });
            var enrolment = await enrolResponse.Content.ReadFromJsonAsync<EnrolmentResponseDto>();

            return (organiser, participant, enrolment!.EnrolmentId);
        }

        [Fact]
        public async Task CaptureResult_AsOwningOrganiser_ReturnsCreated()
        {
            var (organiser, _, enrolmentId) = await SetUpEnrolledParticipantAsync();

            var response = await organiser.PostAsJsonAsync("/api/results", new ResultCreateDto
            {
                EnrolmentId = enrolmentId,
                FinishTime = new TimeSpan(0, 55, 12),
                OverallPosition = 10,
                CategoryPosition = 3,
                CompletionStatus = CompletionStatuses.Finished
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CaptureResult_AsParticipant_ReturnsForbidden()
        {
            // Participants must not be able to capture their own results.
            var (_, participant, enrolmentId) = await SetUpEnrolledParticipantAsync();

            var response = await participant.PostAsJsonAsync("/api/results", new ResultCreateDto
            {
                EnrolmentId = enrolmentId,
                CompletionStatus = CompletionStatuses.Finished
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CaptureResult_WithInvalidCompletionStatus_ReturnsBadRequest()
        {
            var (organiser, _, enrolmentId) = await SetUpEnrolledParticipantAsync();

            var response = await organiser.PostAsJsonAsync("/api/results", new ResultCreateDto
            {
                EnrolmentId = enrolmentId,
                CompletionStatus = "StillRunning" // Not a valid status.
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CaptureResult_Twice_ForSameEnrolment_ReturnsConflict()
        {
            var (organiser, _, enrolmentId) = await SetUpEnrolledParticipantAsync();
            var dto = new ResultCreateDto { EnrolmentId = enrolmentId, CompletionStatus = CompletionStatuses.Finished };

            var first = await organiser.PostAsJsonAsync("/api/results", dto);
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await organiser.PostAsJsonAsync("/api/results", dto);
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task GetMyResults_AsParticipant_ReturnsOwnResultsOnly()
        {
            var (organiser, participant, enrolmentId) = await SetUpEnrolledParticipantAsync();
            await organiser.PostAsJsonAsync("/api/results", new ResultCreateDto
            {
                EnrolmentId = enrolmentId,
                OverallPosition = 5,
                CompletionStatus = CompletionStatuses.Finished
            });

            var response = await participant.GetAsync("/api/results/mine");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var results = await response.Content.ReadFromJsonAsync<List<ResultResponseDto>>();
            Assert.Single(results!);
            Assert.Equal(5, results![0].OverallPosition);
        }

        [Fact]
        public async Task CaptureResult_AsNonOwningOrganiser_ReturnsForbidden()
        {
            var (_, _, enrolmentId) = await SetUpEnrolledParticipantAsync();
            var otherOrganiser = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Organiser);

            var response = await otherOrganiser.PostAsJsonAsync("/api/results", new ResultCreateDto
            {
                EnrolmentId = enrolmentId,
                CompletionStatus = CompletionStatuses.Finished
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetMyResults_IncludesEventDateAndTotalFinishers()
        {
            var (organiser, participant, enrolmentId) = await SetUpEnrolledParticipantAsync();
            await organiser.PostAsJsonAsync("/api/results", new ResultCreateDto
            {
                EnrolmentId = enrolmentId,
                FinishTime = new TimeSpan(0, 50, 0),
                OverallPosition = 1,
                CompletionStatus = CompletionStatuses.Finished
            });

            var results = await participant.GetFromJsonAsync<List<ResultResponseDto>>("/api/results/mine");

            Assert.Single(results!);
            Assert.Equal(1, results![0].TotalFinishers);
            Assert.NotEqual(default, results[0].EventDate);
        }
    }
}
