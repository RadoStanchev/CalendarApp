namespace CalendarApp.Models.Friendships
{
    public class FriendViewModel
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }
}
