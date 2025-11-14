using System;
using System.Collections.Generic;

namespace CalendarApp.Services.Meetings.Models
{
    public class UserMeetingsDto
    {
        public IReadOnlyCollection<MeetingSummaryDto> UpcomingMeetings { get; init; } = Array.Empty<MeetingSummaryDto>();

        public IReadOnlyCollection<MeetingSummaryDto> PastMeetings { get; init; } = Array.Empty<MeetingSummaryDto>();
    }
}
