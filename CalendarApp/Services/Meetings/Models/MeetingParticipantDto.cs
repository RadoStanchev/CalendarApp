
namespace CalendarApp.Services.Meetings.Models
{
    public class MeetingParticipantDto
    {
        public Guid ContactId { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public int StatusId { get; set; }

        public bool IsCreator { get; set; }
    }
}
