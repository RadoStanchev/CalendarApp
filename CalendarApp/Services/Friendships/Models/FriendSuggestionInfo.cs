namespace CalendarApp.Services.Friendships.Models
{
    public class FriendSuggestionInfo
    {
        public Guid UserId { get; init; }

        public string FirstName { get; init; } = string.Empty;

        public string LastName { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;

        public int MutualFriendCount { get; init; }
    }
}
