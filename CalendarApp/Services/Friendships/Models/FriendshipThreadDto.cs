using System;

namespace CalendarApp.Services.Friendships.Models
{
    public class FriendshipThreadDto
    {
        public Guid FriendshipId { get; set; }

        public Guid FriendId { get; set; }

        public string? FriendFirstName { get; set; }

        public string? FriendLastName { get; set; }

        public string? FriendEmail { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? LastMessageContent { get; set; }

        public DateTime? LastMessageSentAt { get; set; }
    }
}
