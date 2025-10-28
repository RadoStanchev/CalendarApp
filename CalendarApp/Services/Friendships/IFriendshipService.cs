using CalendarApp.Services.Friendships.Models;

namespace CalendarApp.Services.Friendships
{
    public interface IFriendshipService
    {
        Task<IReadOnlyCollection<FriendInfo>> GetFriendsAsync(Guid userId);

        Task<IReadOnlyCollection<FriendRequestInfo>> GetPendingRequestsAsync(Guid userId);

        Task<IReadOnlyCollection<FriendSuggestionInfo>> GetSuggestionsAsync(Guid userId, int maxSuggestions = 12);

        Task<bool> SendFriendRequestAsync(Guid requesterId, Guid receiverId);

        Task<bool> AcceptFriendRequestAsync(Guid friendshipId, Guid receiverId);

        Task<bool> DeclineFriendRequestAsync(Guid friendshipId, Guid receiverId);

        Task<bool> CancelFriendRequestAsync(Guid friendshipId, Guid requesterId);

        Task<bool> RemoveFriendAsync(Guid userId, Guid friendId);
    }
}
