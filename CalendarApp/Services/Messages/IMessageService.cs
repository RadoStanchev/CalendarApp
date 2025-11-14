using CalendarApp.Services.Messages.Models;
namespace CalendarApp.Services.Messages
{
    public interface IMessageService
    {
        Task EnsureFriendshipAccessAsync(Guid userId, Guid friendshipId);

        Task EnsureMeetingAccessAsync(Guid userId, Guid meetingId);

        Task<ChatMessageDto> SaveMessageAsync(Guid userId, Guid friendshipId, string content);

        Task<ChatMessageDto> SaveMeetingMessageAsync(Guid userId, Guid meetingId, string content);

        Task<IReadOnlyCollection<ChatMessageDto>> GetRecentFriendshipMessagesAsync(Guid userId, Guid friendshipId, int take);

        Task<IReadOnlyCollection<ChatMessageDto>> GetRecentMeetingMessagesAsync(Guid userId, Guid meetingId, int take);
    }
}
