using System;
using System.Collections.Generic;

namespace CalendarApp.Models.Chat
{
    public class ChatViewModel
    {
        public Guid CurrentUserId { get; set; }

        public string CurrentUserName { get; set; } = string.Empty;

        public IReadOnlyList<ChatThreadViewModel> Threads { get; set; } = Array.Empty<ChatThreadViewModel>();

        public Guid? ActiveFriendshipId { get; set; }

        public IReadOnlyList<ChatMessageViewModel> Messages { get; set; } = Array.Empty<ChatMessageViewModel>();
    }
}
