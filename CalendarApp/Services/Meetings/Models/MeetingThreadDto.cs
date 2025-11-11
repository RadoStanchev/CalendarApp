using System;

namespace CalendarApp.Services.Meetings.Models
{
    public class MeetingThreadDto
    {
        public Guid MeetingId { get; set; }

        public string? Description { get; set; }

        public DateTime StartTime { get; set; }

        public string? Location { get; set; }

        public Guid CreatedById { get; set; }

        public string? CreatorFirstName { get; set; }

        public string? CreatorLastName { get; set; }

        public int ParticipantCount { get; set; }

        public string? LastMessageContent { get; set; }

        public DateTime? LastMessageSentAt { get; set; }
    }
}
