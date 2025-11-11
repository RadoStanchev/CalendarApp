using System;

namespace CalendarApp.Services.Friendships.Models
{
    public class FriendSearchResultInfo
    {
        public Guid UserId { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public FriendRelationshipStatus RelationshipStatus { get; set; }

        public Guid? FriendshipId { get; set; }

        public bool IsIncomingRequest { get; set; }
    }
}
