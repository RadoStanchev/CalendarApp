using CalendarApp.Data.Models;

namespace CalendarApp.Services.Meetings.Models
{
    public class MeetingCreateDto
    {
        public DateTime StartTime { get; set; }

        public string? Location { get; set; }

        public string? Description { get; set; }

        public Guid? CategoryId { get; set; }

        public Guid CreatedById { get; set; }

        public IReadOnlyCollection<MeetingParticipantUpdateDto> Participants { get; set; } = Array.Empty<MeetingParticipantUpdateDto>();
    }
}
