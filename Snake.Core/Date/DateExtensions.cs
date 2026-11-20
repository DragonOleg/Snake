using System;
using System.Globalization;

namespace Snake.Core
{
    public static class DateExtensions
    {
        private const string CONST_UniversalDateFormat = "yyyy-MM-dd";
        private const string CONST_OnlyDateFormat = "d";

        public static string ToDbDateFormat(this DateTime dt)
        {
            return dt.ToString(CONST_UniversalDateFormat);
        }

        public static string ToCultureDateFormat(this DateTime? dt, CultureInfo culture)
        {
            if (!dt.HasValue)
            {
                return string.Empty;
            }

            return dt.Value.ToString(CONST_OnlyDateFormat, culture);
        }

        public static string ToCultureDateFormat(this DateTime dt, CultureInfo culture)
        {
            return dt.ToString(CONST_OnlyDateFormat, culture);
        }
    }
}
