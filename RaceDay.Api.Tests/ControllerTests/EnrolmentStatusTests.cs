using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs.Enrolments;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.TestHelpers;
using Xunit;

namespace RaceDay.Api.Tests.ControllerTests
{
    /// <summary>Covers the Pending -> Confirmed enrolment flow.</summary>
    public class EnrolmentStatusTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public EnrolmentStatusTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private async Task<(HttpClient organiser, HttpClient participant, EnrolmentResponseDto enrolment)> SetUpAsync()
        {
            var organiser = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Organiser);
            var ev = await TestDataHelper.CreateEventAsync(organiser);
            var category = await TestDataHelper.CreateCategoryAsync(organiser, ev.EventId);
            var participant = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Participant);
            var enrolment = await TestDataHelper.EnrolAsync(participant, category.CategoryId);
            return (organiser, participant, enrolment);
        }

        [Fact]
        public async Task NewEnrolment_StartsAsPending()
        {
            var (_, _, enrolment) = await SetUpAsync();

            Assert.Equal(EnrolmentStatus.Pending, enrolment.Status);
        }

        [Fact]
        public async Task Confirm_AsOwningOrganiser_SetsStatusToConfirmed()
        {
            var (organiser, participant, enrolment) = await SetUpAsync();

            var response = await organiser.PutAsync($"/api/enrolments/{enrolment.EnrolmentId}/confirm", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var mine = await participant.GetFromJsonAsync<List<EnrolmentResponseDto>>("/api/enrolments/mine");
            Assert.Equal(EnrolmentStatus.Confirmed, mine!.Single(e => e.EnrolmentId == enrolment.EnrolmentId).Status);
        }

        [Fact]
        public async Task Confirm_AsParticipant_ReturnsForbidden()
        {
            var (_, participant, enrolment) = await SetUpAsync();

            var response = await participant.PutAsync($"/api/enrolments/{enrolment.EnrolmentId}/confirm", null);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Confirm_AsNonOwningOrganiser_ReturnsForbidden()
        {
            var (_, _, enrolment) = await SetUpAsync();
            var otherOrganiser = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Organiser);

            var response = await otherOrganiser.PutAsync($"/api/enrolments/{enrolment.EnrolmentId}/confirm", null);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Confirm_CancelledEnrolment_ReturnsBadRequest()
        {
            var (organiser, participant, enrolment) = await SetUpAsync();
            await participant.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}");

            var response = await organiser.PutAsync($"/api/enrolments/{enrolment.EnrolmentId}/confirm", null);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DeleteCategory_WithEnrolments_ReturnsConflict()
        {
            var (organiser, _, enrolment) = await SetUpAsync();

            var response = await organiser.DeleteAsync($"/api/categories/{enrolment.CategoryId}");

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
    }
}
