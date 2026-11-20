using System;
using System.Globalization;

namespace Snake.Core
{
    public static class DateTimeHelper
    {
        public static DateTime ToUTCDateTime(this DateTime dateTime)
        {
            DateTime localDateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
            DateTime utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, TimeZoneInfo.Local);
            return utcDateTime;
        }

        public static DateTime ParseDateWithCulture(this string dateStr, CultureInfo culture)
        {
            return DateTime.ParseExact(dateStr, culture.DateTimeFormat.ShortDatePattern,
                              culture,
                              DateTimeStyles.AdjustToUniversal);
        }

        public static DateTime? SetDateFromQuery(this DateTime? dt, string dtStr, CultureInfo culture)
        {
            if (!dt.HasValue && !string.IsNullOrEmpty(dtStr))
            {
                return dtStr.ParseDateWithCulture(culture);
            }

            return dt;
        }

        public static DateTime? GetDateTimeFromTick(this DateTime? dt, long? ticks)
        {
            if (!dt.HasValue && ticks.HasValue)
            {
                return new DateTime(ticks.Value);
            }

            return dt;
        }

        public static long? GetTicksFromDateTime(this long? ticks, DateTime? dt)
        {
            if (ticks.HasValue && !dt.HasValue)
            {
                return ticks;
            }

            if (dt.HasValue)
            {
                return dt.Value.Ticks;
            }

            return null;
        }

        public static string ToStringUtc(this DateTime time)
        {
            return $"DateTime({time.Ticks}, DateTimeKind.Utc)";
        }
    }
}