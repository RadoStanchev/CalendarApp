using System;
using System.Collections.Generic;

namespace CalendarApp.Models.Chat
{
    public class ChatViewModel
    {
        public Guid CurrentUserId { get; set; }

        public string CurrentUserName { get; set; } = string.Empty;

        public IReadOnlyList<ChatThreadViewModel> FriendshipThreads { get; set; } = Array.Empty<ChatThreadViewModel>();

        public IReadOnlyList<ChatThreadViewModel> MeetingThreads { get; set; } = Array.Empty<ChatThreadViewModel>();

        public ThreadType? ActiveThreadType { get; set; }

        public Guid? ActiveThreadId { get; set; }

        public IReadOnlyList<ChatMessageViewModel> Messages { get; set; } = Array.Empty<ChatMessageViewModel>();
    }
}
