using System;
using System.Globalization;

namespace CalendarApp.Models.Chat
{
    public static class ChatViewModelHelper
    {
        private static readonly string[] AccentPalette = new[]
        {
            "accent-blue",
            "accent-purple",
            "accent-green",
            "accent-orange",
            "accent-teal"
        };

        private static readonly CultureInfo BulgarianCulture = CultureInfo.GetCultureInfo("bg-BG");

        public static string GetAccentClass(Guid key)
        {
            var index = Math.Abs(key.GetHashCode()) % AccentPalette.Length;
            return AccentPalette[index];
        }

        public static string BuildActivityLabel(DateTime? sentAtUtc)
        {
            if (!sentAtUtc.HasValue)
            {
                return "Няма изпратени съобщения";
            }

            var localTime = DateTime.SpecifyKind(sentAtUtc.Value, DateTimeKind.Utc).ToLocalTime();
            return $"Последно съобщение: {localTime.ToString("g", BulgarianCulture)}";
        }

        public static string BuildMeetingTitle(DateTime startTimeUtc, string? location)
        {
            var local = DateTime.SpecifyKind(startTimeUtc, DateTimeKind.Utc).ToLocalTime();
            var dateLabel = local.ToString("dd MMM yyyy", BulgarianCulture);

            if (string.IsNullOrWhiteSpace(location))
            {
                return dateLabel;
            }

            return $"{dateLabel} • {location.Trim()}";
        }

    }
}

