using CalendarApp.Services.Messages.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CalendarApp.Services.Messages
{
    public interface IMessageService
    {
        Task EnsureFriendshipAccessAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken = default);

        Task EnsureMeetingAccessAsync(Guid userId, Guid meetingId, CancellationToken cancellationToken = default);

        Task<ChatMessageDto> SaveMessageAsync(Guid userId, Guid friendshipId, string content, CancellationToken cancellationToken = default);

        Task<ChatMessageDto> SaveMeetingMessageAsync(Guid userId, Guid meetingId, string content, CancellationToken cancellationToken = default);

        Task MarkFriendshipMessagesAsReadAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken = default);

        Task MarkMeetingMessagesAsReadAsync(Guid userId, Guid meetingId, CancellationToken cancellationToken = default);

        string BuildFriendshipGroupName(Guid friendshipId);

        string BuildMeetingGroupName(Guid meetingId);
    }
}
