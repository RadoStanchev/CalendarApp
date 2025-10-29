using CalendarApp.Data.Models;

namespace CalendarApp.Models.Meetings
{
    public class MeetingParticipantDisplayViewModel
    {
        public Guid ContactId { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public ParticipantStatus Status { get; set; }

        public bool IsCreator { get; set; }
    }
}
