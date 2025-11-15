namespace CalendarApp.Models.Friendships
{
    public class FriendSuggestionViewModel
    {
        public Guid UserId { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public int MutualFriendCount { get; set; }

        public string AvatarInitials { get; set; } = string.Empty;
    }
}
