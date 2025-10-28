namespace CalendarApp.Models.Friendships
{
    public class FriendshipsDashboardViewModel
    {
        public IEnumerable<FriendRequestViewModel> IncomingRequests { get; set; } = Enumerable.Empty<FriendRequestViewModel>();

        public IEnumerable<FriendRequestViewModel> SentRequests { get; set; } = Enumerable.Empty<FriendRequestViewModel>();

        public IEnumerable<FriendViewModel> Friends { get; set; } = Enumerable.Empty<FriendViewModel>();

        public IEnumerable<FriendSuggestionViewModel> Suggestions { get; set; } = Enumerable.Empty<FriendSuggestionViewModel>();

        public int TotalFriends => Friends.Count();
    }
}
