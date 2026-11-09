using System.Net.Http.Json;
using RaceDay.Api.DTOs.Categories;
using RaceDay.Api.DTOs.Enrolments;
using RaceDay.Api.DTOs.Events;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests.TestHelpers
{
    /// <summary>Small helpers that create events, categories and enrolments through the real API.</summary>
    public static class TestDataHelper
    {
        public static async Task<EventResponseDto> CreateEventAsync(
            HttpClient organiser, string name = "Helper Event", string eventType = EventTypes.Run, DateTime? date = null, string location = "Test Venue")
        {
            var response = await organiser.PostAsJsonAsync("/api/events", new EventCreateDto
            {
                EventName = name,
                EventDate = date ?? DateTime.UtcNow.AddMonths(1),
                Location = location,
                Distance = 10m,
                EventType = eventType
            });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<EventResponseDto>())!;
        }

        public static async Task<CategoryResponseDto> CreateCategoryAsync(HttpClient organiser, int eventId, int maxParticipants = 50)
        {
            var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new CategoryCreateDto
            {
                CategoryName = "10km",
                DistanceKm = 10m,
                MaxParticipants = maxParticipants,
                EntryFee = 100m
            });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<CategoryResponseDto>())!;
        }

        public static async Task<EnrolmentResponseDto> EnrolAsync(HttpClient participant, int categoryId)
        {
            var response = await participant.PostAsJsonAsync("/api/enrolments", new EnrolmentCreateDto { CategoryId = categoryId });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<EnrolmentResponseDto>())!;
        }

        /// <summary>A tiny byte array that starts with the PNG signature (enough to pass the API's image check).</summary>
        public static byte[] FakePngBytes() => new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52
        };

        public static MultipartFormDataContent FileContent(byte[] bytes, string fileName, string contentType)
        {
            var file = new ByteArrayContent(bytes);
            file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            return new MultipartFormDataContent { { file, "file", fileName } };
        }
    }
}
