using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Results
{
    public class ResultCreateDto
    {
        [Required]
        public int EnrolmentId { get; set; }

        /// <summary>Format "hh:mm:ss". Omit (or send null) when CompletionStatus is DNF or DQ.</summary>
        public TimeSpan? FinishTime { get; set; }

        public int? OverallPosition { get; set; }

        public int? CategoryPosition { get; set; }

        /// <summary>"Finished", "DNF", or "DQ".</summary>
        [Required]
        public string CompletionStatus { get; set; } = string.Empty;
    }

    public class ResultUpdateDto : ResultCreateDto { }

    public class ResultResponseDto
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

        /// <summary>Number of participants who finished this event (CompletionStatus = Finished), e.g. the 312 in "47th of 312".</summary>
        public int TotalFinishers { get; set; }
    }
}
