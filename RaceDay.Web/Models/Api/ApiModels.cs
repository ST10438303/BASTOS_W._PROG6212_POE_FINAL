namespace RaceDay.Web.Models.Api
{
    // These classes describe the JSON the Part 2 API returns. They are plain data
    // carriers - the MVC project contains no business logic of its own.

    public class AuthResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class UserProfile
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class EventDto
    {
        public int EventId { get; set; }
        public int OrganiserId { get; set; }
        public string OrganiserName { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? RouteInfo { get; set; }
        public decimal Distance { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? BannerImageUrl { get; set; }
        public int EnrolmentCount { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public int EventId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal? DistanceKm { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public int MaxParticipants { get; set; }
        public decimal EntryFee { get; set; }
        public int CurrentEnrolmentCount { get; set; }
    }

    public class EnrolmentDto
    {
        public int EnrolmentId { get; set; }
        public int ParticipantId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EnrolmentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? RaceNumber { get; set; }
    }

    public class ResultDto
    {
        public int ResultId { get; set; }
        public int EnrolmentId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public TimeSpan? FinishTime { get; set; }
        public int? OverallPosition { get; set; }
        public int? CategoryPosition { get; set; }
        public string CompletionStatus { get; set; } = string.Empty;
        public int TotalFinishers { get; set; }
    }

    public class BannerUploadResponse
    {
        public string? BannerImageUrl { get; set; }
    }
}
