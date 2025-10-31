using System;

namespace CalendarApp.Models.Chat
{
    public class ChatThreadViewModel
    {
        public Guid ThreadId { get; set; }

        public ThreadType Type { get; set; }

        public Guid? FriendshipId { get; set; }

        public Guid? FriendId { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string SecondaryLabel { get; set; } = string.Empty;

        public string AvatarInitials { get; set; } = string.Empty;

        public string AccentClass { get; set; } = "accent-blue";

        public string LastMessagePreview { get; set; } = string.Empty;

        public DateTime? LastMessageAt { get; set; }

        public string LastActivityLabel { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public MeetingThreadMetadata? Meeting { get; set; }
    }
}
