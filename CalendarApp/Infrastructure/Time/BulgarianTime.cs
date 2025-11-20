using System;
using System.Runtime.InteropServices;

namespace CalendarApp.Infrastructure.Time
{
    public static class BulgarianTime
    {
        private const string WindowsZoneId = "FLE Standard Time";
        private const string LinuxZoneId = "Europe/Sofia";

        private static readonly Lazy<TimeZoneInfo> LazyZone = new(() =>
        {
            var zoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? WindowsZoneId
                : LinuxZoneId;

            return TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        });

        private static TimeZoneInfo Zone => LazyZone.Value;

        public static DateTime UtcNow => DateTime.UtcNow;

        public static DateTime LocalNow => ConvertUtcToLocal(DateTime.UtcNow);

        public static DateTime ConvertLocalToUtc(DateTime localDateTime)
        {
            var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(unspecified, Zone);
        }

        public static DateTime ConvertUtcToLocal(DateTime utcDateTime)
        {
            var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, Zone);
        }
    }
}
