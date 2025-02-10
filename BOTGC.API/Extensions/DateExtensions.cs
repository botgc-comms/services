using System;
using System.Globalization;

namespace BOTGC.API.Extensions
{
    public static class DateTimeExtensions
    {
        public static string ToOrdinalDateString(this DateTime date)
        {
            return $"{date:dddd} {date.Day}{GetOrdinal(date.Day)} {date:MMMM yyyy}";
        }

        private static string GetOrdinal(int day)
        {
            return (day % 100) switch
            {
                11 or 12 or 13 => "th",
                _ => (day % 10) switch
                {
                    1 => "st",
                    2 => "nd",
                    3 => "rd",
                    _ => "th"
                }
            };
        }
    }

}
