using CalendarApp.Data.Models;

namespace CalendarApp.Services.Meetings.Models
{
    public class MeetingParticipantUpdateDto
    {
        public Guid ContactId { get; set; }

        public ParticipantStatus Status { get; set; } = ParticipantStatus.Pending;
    }
}
