using System.Collections.Generic;

namespace CalendarApp.Models.Meetings
{
    public class MeetingListViewModel
    {
        public string? SearchTerm { get; set; }

        public List<MeetingListItemViewModel> UpcomingMeetings { get; set; } = new();

        public List<MeetingListItemViewModel> PastMeetings { get; set; } = new();
    }
}
