using System;
using System.Threading.Tasks;
namespace CalendarApp.Services.MessageSeens
{
    public interface IMessageSeenService
    {
        Task MarkFriendshipMessagesAsSeenAsync(Guid userId, Guid friendshipId);

        Task MarkMeetingMessagesAsSeenAsync(Guid userId, Guid meetingId);
    }
}
