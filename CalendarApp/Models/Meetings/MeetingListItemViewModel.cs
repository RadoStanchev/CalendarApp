using CalendarApp.Data.Models;

namespace CalendarApp.Models.Meetings
{
    public class MeetingListItemViewModel
    {
        public Guid Id { get; set; }

        public DateTime StartTime { get; set; }

        public string? Location { get; set; }

        public string? Description { get; set; }

        public string CreatedByName { get; set; } = string.Empty;

        public bool ViewerIsCreator { get; set; }

        public ParticipantStatus? ViewerStatus { get; set; }

        public int ParticipantCount { get; set; }
    }
}
