using System;

namespace CalendarApp.Models.Chat
{
    public class ChatMessageViewModel
    {
        public Guid Id { get; set; }

        public Guid SenderId { get; set; }

        public string SenderName { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }

        public Guid? FriendshipId { get; set; }

        public Guid? MeetingId { get; set; }
    }
}
