using CalendarApp.Services.Messages.Models;

namespace CalendarApp.Repositories.Messages;

public interface IMessageRepository
{
    Task<bool> HasFriendshipAccessAsync(Guid userId, Guid friendshipId);
    Task<bool> HasMeetingAccessAsync(Guid userId, Guid meetingId);
    Task<(Guid Id, string? FirstName, string? LastName)?> GetSenderAsync(Guid userId);
    Task<(DateTime StartTime, string? Location)?> GetMeetingInfoAsync(Guid meetingId);
    Task<Guid> InsertAsync(Guid senderId, string content, Guid? friendshipId = null, Guid? meetingId = null);
    Task<IReadOnlyCollection<ChatMessageDto>> GetRecentFriendshipMessagesAsync(Guid friendshipId, int take);
    Task<IReadOnlyCollection<ChatMessageDto>> GetRecentMeetingMessagesAsync(Guid meetingId, int take);
}
