using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    /// <summary>
    /// A road running, walking, or cycling event created and owned by an Organiser.
    ///
    /// NOTE - deviation from the Part 1 ERD:
    /// Part 2's functional requirements state that "Each event must capture a name,
    /// description, date, location, distance, and event type (run, walk, or cycle)".
    /// The Part 1 ERD only held Distance at the Category level. Distance and EventType
    /// have therefore been added here as well, to represent the event's overall
    /// headline distance and discipline, while Categories still hold their own more
    /// specific distance (e.g. a "Marathon" event of type Run might offer both a
    /// 42.2km and a 21.1km category). This change is explained here and in README.md.
    /// </summary>
    public class Event
    {
        [Key]
        public int EventId { get; set; }

        [Required]
        [ForeignKey(nameof(Organiser))]
        public int OrganiserId { get; set; }

        public User? Organiser { get; set; }

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

        /// <summary>Headline distance of the event in kilometres, e.g. 42.2 for a marathon.</summary>
        [Column(TypeName = "decimal(6,2)")]
        public decimal Distance { get; set; }

        /// <summary>Public URL of the event banner image stored in Azure Blob Storage (null if none uploaded).</summary>
        [MaxLength(500)]
        public string? BannerImageUrl { get; set; }

        /// <summary>One of "Run", "Walk", or "Cycle". Validated in EventType constants below.</summary>
        [Required, MaxLength(10)]
        public string EventType { get; set; } = string.Empty;

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }

    public static class EventTypes
    {
        public const string Run = "Run";
        public const string Walk = "Walk";
        public const string Cycle = "Cycle";

        public static readonly string[] All = { Run, Walk, Cycle };

        public static bool IsValid(string? value) =>
            !string.IsNullOrWhiteSpace(value) && All.Contains(value, StringComparer.OrdinalIgnoreCase);
    }
}
