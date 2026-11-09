using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    /// <summary>
    /// A category within an event that a Participant can enrol into, e.g.
    /// "10km", "21.1km", "Under 20", or "Senior". Part 2 requires support for both
    /// age-based and distance-based categories, so DistanceKm is now nullable
    /// (populated for distance categories) and MinAge/MaxAge are nullable
    /// (populated for age-based categories).
    /// </summary>
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [ForeignKey(nameof(Event))]
        public int EventId { get; set; }

        public Event? Event { get; set; }

        [Required, MaxLength(50)]
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>Distance in kilometres. Null for purely age-based categories.</summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal? DistanceKm { get; set; }

        /// <summary>Lower age bound (inclusive). Null if this is a distance-only category.</summary>
        public int? MinAge { get; set; }

        /// <summary>Upper age bound (inclusive). Null if this is a distance-only category or open-ended.</summary>
        public int? MaxAge { get; set; }

        public int MaxParticipants { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        public decimal EntryFee { get; set; }

        // Navigation properties
        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}
