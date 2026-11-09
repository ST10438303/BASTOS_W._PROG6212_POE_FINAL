using System.Globalization;

namespace RaceDay.Web.Helpers
{
    /// <summary>Presentation helpers used by the views (formatting only - no business rules).</summary>
    public static class Formatters
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        /// <summary>1 -> "1st", 2 -> "2nd", 47 -> "47th", 111 -> "111th".</summary>
        public static string Ordinal(int number)
        {
            var lastTwo = number % 100;
            if (lastTwo is >= 11 and <= 13) return number.ToString(Invariant) + "th";

            return (number % 10) switch
            {
                1 => number.ToString(Invariant) + "st",
                2 => number.ToString(Invariant) + "nd",
                3 => number.ToString(Invariant) + "rd",
                _ => number.ToString(Invariant) + "th"
            };
        }

        public static string Date(DateTime date) => date.ToString("dd MMM yyyy", Invariant);

        public static string LongDate(DateTime date) => date.ToString("dddd, d MMMM yyyy", Invariant);

        public static string Money(decimal amount) =>
            amount == 0 ? "Free" : "R" + amount.ToString("0.00", Invariant);

        public static string Km(decimal km) => km.ToString("0.##", Invariant) + " km";

        public static string FinishTime(TimeSpan? time) =>
            time.HasValue ? time.Value.ToString(@"hh\:mm\:ss", Invariant) : "-";

        /// <summary>Value for a finish-time text box: "01:45:30", or empty when there is no time.</summary>
        public static string FinishTimeInput(TimeSpan? time) =>
            time.HasValue ? time.Value.ToString(@"hh\:mm\:ss", Invariant) : string.Empty;

        /// <summary>"Today", "Tomorrow", "In 12 days", "3 days ago" ...</summary>
        public static string Relative(DateTime date)
        {
            var days = (date.Date - DateTime.UtcNow.Date).Days;
            return days switch
            {
                0 => "Today",
                1 => "Tomorrow",
                -1 => "Yesterday",
                > 1 => $"In {days} days",
                _ => $"{-days} days ago"
            };
        }

        /// <summary>Describes a category's age range, e.g. "Over 40", "Under 20", "18-34 years".</summary>
        public static string AgeRange(int? minAge, int? maxAge)
        {
            if (minAge.HasValue && maxAge.HasValue) return $"{minAge}-{maxAge} years";
            if (minAge.HasValue) return $"Over {minAge}";
            if (maxAge.HasValue) return $"Under {maxAge}";
            return string.Empty;
        }

        public static string TypeIcon(string eventType) => eventType switch
        {
            "Run" => "bi-lightning-charge-fill",
            "Walk" => "bi-person-walking",
            "Cycle" => "bi-bicycle",
            _ => "bi-flag-fill"
        };
    }
}
