using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Categories
{
    public class CategoryCreateDto
    {
        [Required, MaxLength(50)]
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>Distance in km. Omit for a purely age-based category.</summary>
        [Range(0.1, 500)]
        public decimal? DistanceKm { get; set; }

        /// <summary>Omit for a purely distance-based category.</summary>
        [Range(0, 120)]
        public int? MinAge { get; set; }

        [Range(0, 120)]
        public int? MaxAge { get; set; }

        [Range(0, 100000)]
        public int MaxParticipants { get; set; }

        [Range(0, 100000)]
        public decimal EntryFee { get; set; }
    }

    public class CategoryResponseDto
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
}
