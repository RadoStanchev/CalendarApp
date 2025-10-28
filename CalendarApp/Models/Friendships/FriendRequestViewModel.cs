namespace CalendarApp.Models.Friendships
{
    public class FriendRequestViewModel
    {
        public Guid FriendshipId { get; set; }

        public Guid UserId { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTime RequestedOn { get; set; }

        public bool IsIncoming { get; set; }
    }
}
