using System;
using CalendarApp.Infrastructure.Time;

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

            var localTime = BulgarianTime.ConvertUtcToLocal(DateTime.SpecifyKind(sentAtUtc.Value, DateTimeKind.Utc));
            return $"Последно съобщение: {localTime.ToString("g")}";
        }

        public static string BuildMeetingTitle(DateTime startTimeUtc, string? location)
        {
            var local = BulgarianTime.ConvertUtcToLocal(DateTime.SpecifyKind(startTimeUtc, DateTimeKind.Utc));
            var dateLabel = local.ToString("dd MMM yyyy");

            if (string.IsNullOrWhiteSpace(location))
            {
                return dateLabel;
            }

            return $"{dateLabel} • {location.Trim()}";
        }

    }
}

