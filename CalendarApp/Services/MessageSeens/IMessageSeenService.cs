using System;
using System.Threading;
using System.Threading.Tasks;

namespace CalendarApp.Services.MessageSeens
{
    public interface IMessageSeenService
    {
        Task MarkFriendshipMessagesAsSeenAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken = default);

        Task MarkMeetingMessagesAsSeenAsync(Guid userId, Guid meetingId, CancellationToken cancellationToken = default);
    }
}
