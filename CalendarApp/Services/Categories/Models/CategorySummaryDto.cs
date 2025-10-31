namespace CalendarApp.Services.Categories.Models
{
    public class CategorySummaryDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Color { get; set; }

        public int MeetingCount { get; set; }
    }
}
