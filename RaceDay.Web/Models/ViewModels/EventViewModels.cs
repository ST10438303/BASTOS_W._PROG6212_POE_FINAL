using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models.ViewModels
{
    /// <summary>Input for the shared Organiser event tab bar.</summary>
    public record EventTabsModel(EventDto Event, string Active);

    // ---------- Participant / public ----------

    public class HomeIndexViewModel
    {
        public string? Search { get; set; }
        public string? EventType { get; set; }
        public bool UpcomingOnly { get; set; } = true;
        public List<EventDto> Events { get; set; } = new();
        public bool HasFilters => !string.IsNullOrWhiteSpace(Search) || !string.IsNullOrWhiteSpace(EventType) || !UpcomingOnly;
    }

    public class EventDetailsViewModel
    {
        public EventDto Event { get; set; } = new();
        public List<CategoryDto> Categories { get; set; } = new();

        /// <summary>The logged-in Participant's own enrolments for THIS event, keyed by category id.</summary>
        public Dictionary<int, EnrolmentDto> MyEnrolments { get; set; } = new();

        public bool IsPast => Event.EventDate.Date < DateTime.UtcNow.Date;
    }

    // ---------- Organiser ----------

    public class DashboardViewModel
    {
        public List<EventDto> Events { get; set; } = new();
        public List<EventDto> Upcoming { get; set; } = new();
        public int TotalEvents => Events.Count;
        public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
        public EventDto? NextEvent => Upcoming.FirstOrDefault();
    }

    public class EventFormViewModel
    {
        public int? EventId { get; set; }

        [Required(ErrorMessage = "Please enter the event name."), StringLength(150)]
        [Display(Name = "Event name")]
        public string EventName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Please choose the event date.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Event date")]
        public DateTime? EventDate { get; set; }

        [Required(ErrorMessage = "Please enter the location."), StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Route information")]
        public string? RouteInfo { get; set; }

        [Required(ErrorMessage = "Please enter the distance in kilometres.")]
        [Range(0.1, 10000, ErrorMessage = "The distance must be between 0.1 and 10 000 km.")]
        [Display(Name = "Distance (km)")]
        public decimal? Distance { get; set; }

        [Required(ErrorMessage = "Please choose the event type.")]
        [RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "Please choose Run, Walk or Cycle.")]
        [Display(Name = "Event type")]
        public string EventType { get; set; } = string.Empty;

        [Display(Name = "Banner image")]
        public IFormFile? BannerFile { get; set; }

        /// <summary>Current banner (edit page only) - display only.</summary>
        public string? BannerImageUrl { get; set; }
    }

    public class CategoryFormViewModel : IValidatableObject
    {
        public int EventId { get; set; }
        public int? CategoryId { get; set; }

        [Required(ErrorMessage = "Please enter a category name, e.g. 10km or Under 20."), StringLength(50)]
        [Display(Name = "Category name")]
        public string CategoryName { get; set; } = string.Empty;

        [Range(0.1, 500, ErrorMessage = "The distance must be between 0.1 and 500 km.")]
        [Display(Name = "Distance (km)")]
        public decimal? DistanceKm { get; set; }

        [Range(0, 120, ErrorMessage = "Please enter an age between 0 and 120.")]
        [Display(Name = "Minimum age")]
        public int? MinAge { get; set; }

        [Range(0, 120, ErrorMessage = "Please enter an age between 0 and 120.")]
        [Display(Name = "Maximum age")]
        public int? MaxAge { get; set; }

        [Required(ErrorMessage = "Please enter the maximum number of participants (0 for no limit).")]
        [Range(0, 100000, ErrorMessage = "Please enter a number between 0 and 100 000.")]
        [Display(Name = "Maximum participants")]
        public int? MaxParticipants { get; set; }

        [Required(ErrorMessage = "Please enter the entry fee (0 for free).")]
        [Range(0, 100000, ErrorMessage = "Please enter an amount between 0 and 100 000.")]
        [Display(Name = "Entry fee (R)")]
        public decimal? EntryFee { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!DistanceKm.HasValue && !MinAge.HasValue && !MaxAge.HasValue)
            {
                yield return new ValidationResult(
                    "Add a distance, an age range, or both to define this category.",
                    new[] { nameof(DistanceKm) });
            }

            if (MinAge.HasValue && MaxAge.HasValue && MinAge > MaxAge)
            {
                yield return new ValidationResult(
                    "The minimum age cannot be higher than the maximum age.",
                    new[] { nameof(MaxAge) });
            }
        }
    }

    public class CategoriesPageViewModel
    {
        public EventDto Event { get; set; } = new();
        public List<CategoryDto> Categories { get; set; } = new();
        public CategoryFormViewModel NewCategory { get; set; } = new();
    }

    public class EnrolmentsPageViewModel
    {
        public EventDto Event { get; set; } = new();
        public List<EnrolmentDto> Enrolments { get; set; } = new();
    }

    // ---------- Results ----------

    public class ResultRowViewModel
    {
        public EnrolmentDto Enrolment { get; set; } = new();
        public ResultDto? ExistingResult { get; set; }
    }

    public class ResultsEntryPageViewModel
    {
        public EventDto Event { get; set; } = new();
        public List<ResultRowViewModel> Rows { get; set; } = new();
    }

    public class ResultFormViewModel
    {
        public int EventId { get; set; }
        public int EnrolmentId { get; set; }
        public int? ResultId { get; set; }

        /// <summary>Entered as hh:mm:ss.</summary>
        public string? FinishTime { get; set; }

        [Range(1, 1000000, ErrorMessage = "The overall position must be 1 or more.")]
        public int? OverallPosition { get; set; }

        [Range(1, 1000000, ErrorMessage = "The category position must be 1 or more.")]
        public int? CategoryPosition { get; set; }

        [Required]
        [RegularExpression("^(Finished|DNF|DQ)$", ErrorMessage = "Please choose Finished, DNF or DQ.")]
        public string CompletionStatus { get; set; } = "Finished";
    }
}
