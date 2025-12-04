using System;
using System.Text.RegularExpressions;

namespace BOTGC.API.Common
{
    public static class DateHelper
    {
    public static readonly HashSet<DateTime> UkBankHolidays =
        BuildUkBankHolidays(DateTime.Today.Year, 8);

    public static DateTime NextWorkingDay(DateTime from)
    {
        var d = from.Date;
        do
        {
            d = d.AddDays(1);
        }
        while (IsWeekend(d) || UkBankHolidays.Contains(d));
        return d;
    }

    public static long UtcTicks(this DateTimeOffset dto) => dto.UtcDateTime.Ticks;

    public static bool IsNonWorkingDay(DateTime date)
    {
        var d = date.Date;
        return IsWeekend(d) || UkBankHolidays.Contains(d);
    }

    private static bool IsWeekend(DateTime d) =>
        d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday;

    private static HashSet<DateTime> BuildUkBankHolidays(int startYear, int years)
    {
        var set = new HashSet<DateTime>();
        for (var y = startYear; y < startYear + years; y++)
        {
            // New Year’s Day (with substitute)
            set.Add(AdjustForWeekend(new DateTime(y, 1, 1)));

            // Good Friday / Easter Monday
            var easter = EasterSunday(y);
            set.Add(easter.AddDays(-2).Date);
            set.Add(easter.AddDays(1).Date);

            // Early May Bank Holiday – first Monday in May
            set.Add(FirstMondayInMonth(y, 5));

            // Spring Bank Holiday – last Monday in May
            set.Add(LastMondayInMonth(y, 5));

            // Summer Bank Holiday – last Monday in August
            set.Add(LastMondayInMonth(y, 8));

            // Christmas Day and Boxing Day with substitute rules
            var christmas = new DateTime(y, 12, 25);
            var boxing = new DateTime(y, 12, 26);

            if (christmas.DayOfWeek == DayOfWeek.Saturday)
            {
                // Christmas on Sat -> Mon 27 substitute; Boxing on Sun -> Tue 28 substitute
                set.Add(new DateTime(y, 12, 27));
                set.Add(new DateTime(y, 12, 28));
            }
            else if (christmas.DayOfWeek == DayOfWeek.Sunday)
            {
                // Christmas on Sun -> Tue 27 substitute; Boxing on Mon 26 as normal
                set.Add(new DateTime(y, 12, 26));
                set.Add(new DateTime(y, 12, 27));
            }
            else if (boxing.DayOfWeek == DayOfWeek.Saturday)
            {
                // Christmas Fri 25, Boxing Sat 26 -> Mon 28 substitute for Boxing
                set.Add(christmas);
                set.Add(new DateTime(y, 12, 28));
            }
            else if (boxing.DayOfWeek == DayOfWeek.Sunday)
            {
                // Christmas Sat handled above; Christmas not Sun -> Mon 27 substitute for Boxing
                set.Add(christmas);
                set.Add(new DateTime(y, 12, 27));
            }
            else
            {
                set.Add(christmas);
                set.Add(boxing);
            }
        }
        return set;
    }

    private static DateTime AdjustForWeekend(DateTime d)
    {
        return d.DayOfWeek switch
        {
            DayOfWeek.Saturday => d.AddDays(2).Date,
            DayOfWeek.Sunday => d.AddDays(1).Date,
            _ => d.Date
        };
    }

    // Anonymous Gregorian algorithm for Western Easter Sunday
    private static DateTime EasterSunday(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var month = (h + l - 7 * m + 114) / 31;
        var day = ((h + l - 7 * m + 114) % 31) + 1;
        return new DateTime(year, month, day);
    }

    private static DateTime FirstMondayInMonth(int year, int month)
    {
        var d = new DateTime(year, month, 1);
        while (d.DayOfWeek != DayOfWeek.Monday) d = d.AddDays(1);
        return d.Date;
    }

    private static DateTime LastMondayInMonth(int year, int month)
    {
        var d = new DateTime(year, month, DateTime.DaysInMonth(year, month));
        while (d.DayOfWeek != DayOfWeek.Monday) d = d.AddDays(-1);
        return d.Date;
    }

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
