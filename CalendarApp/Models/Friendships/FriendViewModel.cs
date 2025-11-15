namespace CalendarApp.Models.Friendships
{
    public class FriendViewModel
    {
        public Guid FriendshipId { get; set; }

        public Guid Id { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string AvatarInitials { get; set; } = string.Empty;
    }
}
