using CalendarApp.Services.Friendships.Models;

namespace CalendarApp.Repositories.Friendships;

public interface IFriendshipRepository
{
    Task<IReadOnlyCollection<FriendshipThreadDto>> GetChatThreadsAsync(Guid userId);
    Task<FriendshipThreadDto?> GetChatThreadAsync(Guid friendshipId, Guid userId);
    Task<IReadOnlyCollection<FriendInfo>> GetFriendsAsync(Guid userId);
    Task<IReadOnlyCollection<FriendRequestInfo>> GetPendingRequestsAsync(Guid userId);
    Task<IReadOnlyCollection<FriendSuggestionInfo>> GetSuggestionsAsync(Guid userId, int maxSuggestions = 12);
    Task<IReadOnlyCollection<FriendSearchResultInfo>> SearchAsync(Guid userId, string term, IEnumerable<Guid> excludeIds);

    Task<(bool Success, Guid? FriendshipId)> SendFriendRequestAsync(Guid requesterId, Guid receiverId);
    Task<(bool Success, Guid? RequesterId)> AcceptFriendRequestAsync(Guid friendshipId, Guid receiverId);
    Task<(bool Success, Guid? RequesterId)> DeclineFriendRequestAsync(Guid friendshipId, Guid receiverId);
    Task<(bool Success, Guid? ReceiverId)> CancelFriendRequestAsync(Guid friendshipId, Guid requesterId);
    Task<bool> RemoveFriendAsync(Guid friendshipId, Guid cancelerId);

    Task<string?> GetContactFullNameAsync(Guid userId);
}
