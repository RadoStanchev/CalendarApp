using CalendarApp.Data.Models;

namespace CalendarApp.Models.Meetings
{
    public class MeetingDetailsViewModel
    {
        public Guid Id { get; set; }

        public DateTime StartTime { get; set; }

        public string? Location { get; set; }

        public string? Description { get; set; }

        public string CreatedByName { get; set; } = string.Empty;

        public Guid ViewerId { get; set; }

        public bool ViewerIsCreator { get; set; }

        public bool ViewerIsParticipant { get; set; }

        public ParticipantStatus? ViewerStatus { get; set; }

        public List<MeetingParticipantDisplayViewModel> Participants { get; set; } = new();
    }
}
