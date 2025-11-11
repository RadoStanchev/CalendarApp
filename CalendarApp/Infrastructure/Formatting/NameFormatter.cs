using System.Linq;

namespace CalendarApp.Infrastructure.Formatting
{
    public static class NameFormatter
    {
        public static string Format(string? firstName, string? lastName)
        {
            var parts = new[] { firstName, lastName }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            return parts.Length > 0 ? string.Join(" ", parts) : "Unknown";
        }
    }
}
