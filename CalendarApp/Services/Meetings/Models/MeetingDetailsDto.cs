using CalendarApp.Data.Models;

namespace CalendarApp.Services.Meetings.Models
{
    public class MeetingDetailsDto
    {
        public Guid Id { get; set; }

        public DateTime StartTime { get; set; }

        public string? Location { get; set; }

        public string? Description { get; set; }

        public Guid CreatedById { get; set; }

        public string CreatedByName { get; set; } = string.Empty;

        public Guid ViewerId { get; set; }

        public bool ViewerIsCreator { get; set; }

        public bool ViewerIsParticipant { get; set; }

        public ParticipantStatus? ViewerStatus { get; set; }

        public IReadOnlyCollection<MeetingParticipantDto> Participants { get; set; } = Array.Empty<MeetingParticipantDto>();
    }
}
