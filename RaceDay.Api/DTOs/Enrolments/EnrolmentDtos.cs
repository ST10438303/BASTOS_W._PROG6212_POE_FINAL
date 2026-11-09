using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Enrolments
{
    public class EnrolmentCreateDto
    {
        [Required]
        public int CategoryId { get; set; }
    }

    public class EnrolmentResponseDto
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
}
