namespace CalendarApp.Services.Meetings.Models
{
    public class ContactSummaryDto
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }
}
