using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Events
{
    public class EventCreateDto
    {
        [Required, MaxLength(150)]
        public string EventName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required, MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? RouteInfo { get; set; }

        [Range(0.1, 10000)]
        public decimal Distance { get; set; }

        /// <summary>"Run", "Walk", or "Cycle".</summary>
        [Required]
        public string EventType { get; set; } = string.Empty;
    }

    public class EventUpdateDto : EventCreateDto { }

    public class EventResponseDto
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

        /// <summary>Number of non-cancelled enrolments. Only populated by GET /api/events/mine.</summary>
        public int EnrolmentCount { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
