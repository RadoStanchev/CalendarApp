using System;

namespace CalendarApp.Models.Chat
{
    public class ChatThreadViewModel
    {
        public Guid FriendshipId { get; set; }

        public Guid FriendId { get; set; }

        public string FriendName { get; set; } = string.Empty;

        public string FriendEmail { get; set; } = string.Empty;

        public string AvatarInitials { get; set; } = string.Empty;

        public string AccentClass { get; set; } = "accent-blue";

        public string LastMessagePreview { get; set; } = string.Empty;

        public DateTime? LastMessageAt { get; set; }

        public string LastActivityLabel { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
