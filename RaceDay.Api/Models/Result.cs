using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    /// <summary>
    /// A finishing result captured by an Organiser for a Participant's Enrolment.
    /// There is a strict 1:1 relationship between Enrolment and Result.
    /// </summary>
    public class Result
    {
        [Key]
        public int ResultId { get; set; }

        [Required]
        public int EnrolmentId { get; set; }

        [ForeignKey(nameof(EnrolmentId))]
        public Enrolment? Enrolment { get; set; }

        [Required]
        [ForeignKey(nameof(CapturedBy))]
        public int CapturedByUserId { get; set; }

        public User? CapturedBy { get; set; }

        public TimeSpan? FinishTime { get; set; }

        public int? OverallPosition { get; set; }

        public int? CategoryPosition { get; set; }

        [Required, MaxLength(20)]
        public string CompletionStatus { get; set; } = CompletionStatuses.Finished;
    }

    /// <summary>
    /// Named "CompletionStatuses" (plural) rather than "CompletionStatus" to avoid
    /// colliding with the Result.CompletionStatus property of the same name within
    /// this namespace, which would otherwise be ambiguous at usage sites.
    /// </summary>
    public static class CompletionStatuses
    {
        public const string Finished = "Finished";
        public const string DNF = "DNF";
        public const string DQ = "DQ";
    }
}
