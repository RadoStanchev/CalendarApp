using System;
using System.Globalization;
using System.Linq;
using System.Text;

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

        public static string BuildInitials(string? firstName, string? lastName)
        {
            var builder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(firstName))
            {
                builder.Append(char.ToUpper(firstName![0], BulgarianCulture));
            }

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                builder.Append(char.ToUpper(lastName![0], BulgarianCulture));
            }

            return builder.Length == 0 ? "?" : builder.ToString();
        }

        public static string BuildFullName(string? firstName, string? lastName)
        {
            var first = firstName?.Trim() ?? string.Empty;
            var last = lastName?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(first) && string.IsNullOrEmpty(last))
            {
                return string.Empty;
            }

            return string.Join(" ", new[] { first, last }.Where(part => !string.IsNullOrEmpty(part)));
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

        public static string BuildMeetingSubtitle(DateTime startTimeUtc, string? location)
        {
            var local = DateTime.SpecifyKind(startTimeUtc, DateTimeKind.Utc).ToLocalTime();
            var formatted = local.ToString("g", BulgarianCulture);

            if (string.IsNullOrWhiteSpace(location))
            {
                return formatted;
            }

            return $"{formatted} • {location.Trim()}";
        }
    }
}

