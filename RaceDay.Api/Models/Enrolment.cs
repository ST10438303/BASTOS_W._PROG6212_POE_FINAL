using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    /// <summary>
    /// Records that a Participant has entered a specific Category of an Event.
    /// </summary>
    public class Enrolment
    {
        [Key]
        public int EnrolmentId { get; set; }

        [Required]
        [ForeignKey(nameof(Participant))]
        public int ParticipantId { get; set; }

        public User? Participant { get; set; }

        [Required]
        [ForeignKey(nameof(Category))]
        public int CategoryId { get; set; }

        public Category? Category { get; set; }

        public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(20)]
        public string Status { get; set; } = EnrolmentStatus.Pending;

        [MaxLength(20)]
        public string? RaceNumber { get; set; }

        // Navigation property (0..1 Result per Enrolment)
        public Result? Result { get; set; }
    }

    public static class EnrolmentStatus
    {
        /// <summary>Newly submitted enrolment awaiting the Organiser's confirmation.</summary>
        public const string Pending = "Pending";
        /// <summary>Enrolment confirmed by the event's Organiser.</summary>
        public const string Confirmed = "Confirmed";
        public const string Cancelled = "Cancelled";
    }
}
