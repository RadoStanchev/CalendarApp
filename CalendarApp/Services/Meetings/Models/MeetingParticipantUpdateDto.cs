
namespace CalendarApp.Services.Meetings.Models
{
    public class MeetingParticipantUpdateDto
    {
        public Guid ContactId { get; set; }

        public int StatusId { get; set; } = 0;
    }
}
