namespace CalendarApp.Services.Friendships.Models
{
    public class FriendRequestInfo
    {
        public Guid FriendshipId { get; init; }

        public Guid RequesterId { get; init; }

        public Guid ReceiverId { get; init; }

        public Guid TargetUserId { get; init; }

        public string TargetFirstName { get; init; } = string.Empty;

        public string TargetLastName { get; init; } = string.Empty;

        public string TargetEmail { get; init; } = string.Empty;

        public DateTime CreatedAt { get; init; }

        public bool IsIncoming { get; init; }
    }
}
