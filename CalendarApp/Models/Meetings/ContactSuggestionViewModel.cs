namespace CalendarApp.Models.Meetings
{
    public class ContactSuggestionViewModel
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }
}
