using System;

namespace CalendarApp.Services.Messages.Models
{
    public class ChatMessageDto
    {
        public Guid FriendshipId { get; set; }

        public Guid MessageId { get; set; }

        public Guid SenderId { get; set; }

        public string? SenderName { get; set; }

        public string Content { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }
    }
}
