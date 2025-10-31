using System;

namespace CalendarApp.Models.Chat
{
    public class MeetingThreadMetadata
    {
        public Guid MeetingId { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime StartTimeUtc { get; set; }

        public string? Location { get; set; }

        public bool IsOrganizer { get; set; }

        public int ParticipantCount { get; set; }
    }
}
