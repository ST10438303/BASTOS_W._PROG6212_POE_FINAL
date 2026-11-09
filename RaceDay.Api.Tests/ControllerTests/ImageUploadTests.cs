using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs.Events;
using RaceDay.Api.DTOs.Users;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.TestHelpers;
using Xunit;

namespace RaceDay.Api.Tests.ControllerTests
{
    /// <summary>Covers banner and profile picture uploads (Azure Blob Storage is replaced by a fake).</summary>
    public class ImageUploadTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ImageUploadTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task UploadBanner_AsOwningOrganiser_StoresUrlOnEvent()
        {
            var organiser = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Organiser);
            var ev = await TestDataHelper.CreateEventAsync(organiser);

            var response = await organiser.PostAsync(
                $"/api/events/{ev.EventId}/banner",
                TestDataHelper.FileContent(TestDataHelper.FakePngBytes(), "banner.png", "image/png"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var fetched = await organiser.GetFromJsonAsync<EventResponseDto>($"/api/events/{ev.EventId}");
            Assert.NotNull(fetched!.BannerImageUrl);
            Assert.Contains("/banners/", fetched.BannerImageUrl);
        }

        [Fact]
        public async Task UploadBanner_AsParticipant_ReturnsForbidden()
        {
            var organiser = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Organiser);
            var ev = await TestDataHelper.CreateEventAsync(organiser);
            var participant = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Participant);

            var response = await participant.PostAsync(
                $"/api/events/{ev.EventId}/banner",
                TestDataHelper.FileContent(TestDataHelper.FakePngBytes(), "banner.png", "image/png"));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UploadBanner_AsNonOwningOrganiser_ReturnsForbidden()
        {
            var owner = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Organiser);
            var ev = await TestDataHelper.CreateEventAsync(owner);
            var other = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Organiser);

            var response = await other.PostAsync(
                $"/api/events/{ev.EventId}/banner",
                TestDataHelper.FileContent(TestDataHelper.FakePngBytes(), "banner.png", "image/png"));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UploadBanner_WithNonImageFile_ReturnsBadRequest()
        {
            var organiser = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Organiser);
            var ev = await TestDataHelper.CreateEventAsync(organiser);
            var notAnImage = System.Text.Encoding.UTF8.GetBytes("this is plainly not an image file");

            var response = await organiser.PostAsync(
                $"/api/events/{ev.EventId}/banner",
                TestDataHelper.FileContent(notAnImage, "banner.png", "image/png"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UploadProfilePicture_AsLoggedInUser_StoresUrlOnProfile()
        {
            var participant = await TestAuthHelper.RegisterAndLoginAsync(_factory, RoleNames.Participant);

            var response = await participant.PostAsync(
                "/api/users/me/picture",
                TestDataHelper.FileContent(TestDataHelper.FakePngBytes(), "me.png", "image/png"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var profile = await participant.GetFromJsonAsync<UserProfileDto>("/api/users/me");
            Assert.NotNull(profile!.ProfilePictureUrl);
            Assert.Contains("/profiles/", profile.ProfilePictureUrl);
        }

        [Fact]
        public async Task UploadProfilePicture_WithoutLoggingIn_ReturnsUnauthorized()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsync(
                "/api/users/me/picture",
                TestDataHelper.FileContent(TestDataHelper.FakePngBytes(), "me.png", "image/png"));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
