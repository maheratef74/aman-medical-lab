using System;
using System.Globalization;

namespace DrMohamedWeb.Application.Helpers
{
    public static class EgyptTime
    {
        public static readonly TimeZoneInfo Info = GetEgyptTimeZone();

        private static TimeZoneInfo GetEgyptTimeZone()
        {
            try
            {
                var id = OperatingSystem.IsWindows() ? "Egypt Standard Time" : "Africa/Cairo";
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }

        public static DateTime FromUtc(DateTime utc)
        {
            // Treat the value as UTC regardless of Kind (SQL Server returns Unspecified)
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Info);
        }

        public static string Format(DateTime? utc, string dateFormat = "yyyy-MM-dd hh:mm")
        {
            if (!utc.HasValue) return "";

            var local = FromUtc(utc.Value);
            var time = local.ToString(dateFormat, CultureInfo.InvariantCulture);
            var designator = local.Hour < 12 ? "ص" : "م";
            return $"{time} {designator}";
        }
    }
}