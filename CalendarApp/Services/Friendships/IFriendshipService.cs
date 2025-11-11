using CalendarApp.Services.Friendships.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CalendarApp.Services.Friendships
{
    public interface IFriendshipService
    {
        Task<IReadOnlyCollection<FriendInfo>> GetFriendsAsync(Guid userId);

        Task<IReadOnlyCollection<FriendRequestInfo>> GetPendingRequestsAsync(Guid userId);

        Task<IReadOnlyCollection<FriendSuggestionInfo>> GetSuggestionsAsync(Guid userId, int maxSuggestions = 12);

        Task<IReadOnlyCollection<FriendSearchResultInfo>> SearchAsync(Guid userId, string term, IEnumerable<Guid> excludeIds);

        Task<(bool Success, Guid? FriendshipId)> SendFriendRequestAsync(Guid requesterId, Guid receiverId);

        Task<bool> AcceptFriendRequestAsync(Guid friendshipId, Guid receiverId);

        Task<bool> DeclineFriendRequestAsync(Guid friendshipId, Guid receiverId);

        Task<bool> CancelFriendRequestAsync(Guid friendshipId, Guid requesterId);

        Task<bool> RemoveFriendAsync(Guid userId, Guid friendId);

        Task<IReadOnlyCollection<FriendshipThreadDto>> GetChatThreadsAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<FriendshipThreadDto?> GetChatThreadAsync(Guid friendshipId, Guid userId, CancellationToken cancellationToken = default);
    }
}
