using CalendarApp.Services.Messages;
using CalendarApp.Services.MessageSeens.Repositories;

namespace CalendarApp.Services.MessageSeens
{
    public class MessageSeenService : IMessageSeenService
    {
        private readonly IMessageSeenRepository messageSeenRepository;
        private readonly IMessageService messageService;

        public MessageSeenService(IMessageSeenRepository messageSeenRepository, IMessageService messageService)
        {
            this.messageSeenRepository = messageSeenRepository;
            this.messageService = messageService;
        }

        public async Task MarkFriendshipMessagesAsSeenAsync(Guid userId, Guid friendshipId)
        {
            await messageService.EnsureFriendshipAccessAsync(userId, friendshipId);
            var unseenMessageIds = await messageSeenRepository.GetUnseenFriendshipMessageIdsAsync(userId, friendshipId);
            if (unseenMessageIds.Count == 0) return;
            await messageSeenRepository.InsertManyAsync(userId, unseenMessageIds, DateTime.UtcNow);
        }

        public async Task MarkMeetingMessagesAsSeenAsync(Guid userId, Guid meetingId)
        {
            await messageService.EnsureMeetingAccessAsync(userId, meetingId);
            var unseenMessageIds = await messageSeenRepository.GetUnseenMeetingMessageIdsAsync(userId, meetingId);
            if (unseenMessageIds.Count == 0) return;
            await messageSeenRepository.InsertManyAsync(userId, unseenMessageIds, DateTime.UtcNow);
        }
    }
}
