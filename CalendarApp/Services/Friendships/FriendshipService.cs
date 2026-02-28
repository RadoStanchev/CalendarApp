using CalendarApp.Services.Friendships.Models;
using CalendarApp.Services.Friendships.Repositories;
using CalendarApp.Services.Notifications;
using CalendarApp.Services.Notifications.Models;

namespace CalendarApp.Services.Friendships
{
    public class FriendshipService : IFriendshipService
    {
        private readonly IFriendshipRepository friendshipRepository;
        private readonly INotificationService notificationService;

        public FriendshipService(IFriendshipRepository friendshipRepository, INotificationService notificationService)
        {
            this.friendshipRepository = friendshipRepository;
            this.notificationService = notificationService;
        }

        public Task<IReadOnlyCollection<FriendshipThreadDto>> GetChatThreadsAsync(Guid userId)
            => friendshipRepository.GetChatThreadsAsync(userId);

        public Task<FriendshipThreadDto?> GetChatThreadAsync(Guid friendshipId, Guid userId)
            => friendshipRepository.GetChatThreadAsync(friendshipId, userId);

        public Task<IReadOnlyCollection<FriendInfo>> GetFriendsAsync(Guid userId)
            => friendshipRepository.GetFriendsAsync(userId);

        public Task<IReadOnlyCollection<FriendRequestInfo>> GetPendingRequestsAsync(Guid userId)
            => friendshipRepository.GetPendingRequestsAsync(userId);

        public Task<IReadOnlyCollection<FriendSuggestionInfo>> GetSuggestionsAsync(Guid userId, int maxSuggestions = 12)
            => friendshipRepository.GetSuggestionsAsync(userId, maxSuggestions);

        public Task<IReadOnlyCollection<FriendSearchResultInfo>> SearchAsync(Guid userId, string term, IEnumerable<Guid> excludeIds)
            => friendshipRepository.SearchAsync(userId, term, excludeIds);

        public async Task<(bool Success, Guid? FriendshipId)> SendFriendRequestAsync(Guid requesterId, Guid receiverId)
        {
            var result = await friendshipRepository.SendFriendRequestAsync(requesterId, receiverId);
            if (!result.Success)
            {
                return result;
            }

            var requesterName = await friendshipRepository.GetContactFullNameAsync(requesterId);
            if (!string.IsNullOrWhiteSpace(requesterName))
            {
                await notificationService.CreateNotificationAsync(new NotificationCreateDto
                {
                    UserId = receiverId,
                    Message = $"{requesterName} ви изпрати покана за приятелство.",
                    Type = NotificationType.Invitation
                });
            }

            return result;
        }

        public async Task<bool> AcceptFriendRequestAsync(Guid friendshipId, Guid receiverId)
        {
            var result = await friendshipRepository.AcceptFriendRequestAsync(friendshipId, receiverId);
            if (!result.Success || !result.RequesterId.HasValue)
            {
                return false;
            }

            var receiverName = await friendshipRepository.GetContactFullNameAsync(receiverId);
            if (!string.IsNullOrWhiteSpace(receiverName))
            {
                await notificationService.CreateNotificationAsync(new NotificationCreateDto
                {
                    UserId = result.RequesterId.Value,
                    Message = $"{receiverName} прие вашата покана за приятелство.",
                    Type = NotificationType.Info
                });
            }

            return true;
        }

        public async Task<bool> DeclineFriendRequestAsync(Guid friendshipId, Guid receiverId)
        {
            var result = await friendshipRepository.DeclineFriendRequestAsync(friendshipId, receiverId);
            if (!result.Success || !result.RequesterId.HasValue)
            {
                return false;
            }

            var receiverName = await friendshipRepository.GetContactFullNameAsync(receiverId);
            if (!string.IsNullOrWhiteSpace(receiverName))
            {
                await notificationService.CreateNotificationAsync(new NotificationCreateDto
                {
                    UserId = result.RequesterId.Value,
                    Message = $"{receiverName} отказа вашата покана за приятелство.",
                    Type = NotificationType.Warning
                });
            }

            return true;
        }

        public async Task<bool> CancelFriendRequestAsync(Guid friendshipId, Guid requesterId)
        {
            var result = await friendshipRepository.CancelFriendRequestAsync(friendshipId, requesterId);
            if (!result.Success || !result.ReceiverId.HasValue)
            {
                return false;
            }

            var requesterName = await friendshipRepository.GetContactFullNameAsync(requesterId);
            if (!string.IsNullOrWhiteSpace(requesterName))
            {
                await notificationService.CreateNotificationAsync(new NotificationCreateDto
                {
                    UserId = result.ReceiverId.Value,
                    Message = $"{requesterName} отмени поканата за приятелство.",
                    Type = NotificationType.Info
                });
            }

            return true;
        }

        public Task<bool> RemoveFriendAsync(Guid friendshipId, Guid cancelerId)
            => friendshipRepository.RemoveFriendAsync(friendshipId, cancelerId);
    }
}
