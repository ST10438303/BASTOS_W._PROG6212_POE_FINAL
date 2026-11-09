using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    /// <summary>
    /// Represents a registered user of the RaceDay system.
    /// A single table stores both Organisers and Participants, distinguished by Role.
    /// </summary>
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Stores a salted hash of the password only - never the plain text value.
        /// Generated using ASP.NET Core's PasswordHasher&lt;User&gt;.
        /// </summary>
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Either "Organiser" or "Participant". Enforced via RoleNames constants
        /// and a database CHECK constraint configured in RaceDayContext.
        /// </summary>
        [Required, MaxLength(20)]
        public string Role { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        /// <summary>Public URL of the profile picture stored in Azure Blob Storage (null if none uploaded).</summary>
        [MaxLength(500)]
        public string? ProfilePictureUrl { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [InverseProperty(nameof(Event.Organiser))]
        public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();

        [InverseProperty(nameof(Enrolment.Participant))]
        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();

        [InverseProperty(nameof(Result.CapturedBy))]
        public ICollection<Result> ResultsCaptured { get; set; } = new List<Result>();
    }

    /// <summary>
    /// Centralised role name constants so "magic strings" are not repeated
    /// across controllers, authorization attributes, and seed data.
    /// </summary>
    public static class RoleNames
    {
        public const string Organiser = "Organiser";
        public const string Participant = "Participant";
    }
}
