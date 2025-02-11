namespace Services.Models
{
    public class DateRange
    {
        /// <example>2024-01-01</example>
        public required DateTime Start { get; set; }
        /// <example>2024-12-31</example>
        public required DateTime End { get; set; }
    }
}
