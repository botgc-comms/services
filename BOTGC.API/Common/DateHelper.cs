using System;
using System.Text.RegularExpressions;

namespace BOTGC.API.Common
{
    public static class DateHelper
    {
        public static string GetCurrentSubscriptionYear(DateTime? today = null)
        {
            var now = today ?? DateTime.Today;
            int startYear, endYear;

            if (now.Month >= 4) // April or later: year runs from this year to next
            {
                startYear = now.Year % 100;
                endYear = (now.Year + 1) % 100;
            }
            else // Jan, Feb, Mar: year runs from last year to this year
            {
                startYear = (now.Year - 1) % 100;
                endYear = now.Year % 100;
            }

            return $"{startYear:D2}/{endYear:D2}";
        }

        public static string GetSubscriptionYear(DateTime forDate)
        {
            return GetSubscriptionYear(null, forDate);
        }

        public static string GetSubscriptionYear(string? input, DateTime? today = null)
        {
            // Try to extract two 2-digit numbers starting with 2 (e.g., 24/25, 2024-25, sub 24 to 25)
            if (!string.IsNullOrWhiteSpace(input))
            {
                var match = Regex.Match(input, @"(2\d)[^\d]*(2\d)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    int startYear = int.Parse(match.Groups[1].Value);
                    int endYear = int.Parse(match.Groups[2].Value);
                    return $"{startYear:D2}/{endYear:D2}";
                }
            }

            // Fallback: use current date to determine subscription year
            var now = today ?? DateTime.Today;
            int start, end;
            if (now.Month >= 4)
            {
                start = now.Year % 100;
                end = (now.Year + 1) % 100;
            }
            else
            {
                start = (now.Year - 1) % 100;
                end = now.Year % 100;
            }
            return $"{start:D2}/{end:D2}";
        }

        public static (DateTime Start, DateTime End) GetSubscriptionYearRange(string subYear)
        {
            // Fuzzy match: find two 2-digit numbers starting with 2 (e.g., 24/25, 2024-25, sub 24 to 25)
            var match = Regex.Match(subYear, @"(2\d)[^\d]*(2\d)", RegexOptions.IgnoreCase);
            if (!match.Success)
                throw new ArgumentException("Invalid subscription year format.", nameof(subYear));

            int startYear = int.Parse(match.Groups[1].Value);
            int endYear = int.Parse(match.Groups[2].Value);

            // Assume 20xx for both years
            int startFullYear = 2000 + startYear;
            int endFullYear = 2000 + endYear;

            var startDate = new DateTime(startFullYear, 4, 1);
            var endDate = new DateTime(endFullYear, 3, 31);

            return (startDate, endDate);
        }
    }
}
