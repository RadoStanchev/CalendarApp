using CalendarApp.Services.Messages.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CalendarApp.Services.Messages
{
    public interface IMessageService
    {
        Task EnsureFriendshipAccessAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken = default);

        Task<ChatMessageDto> SaveMessageAsync(Guid userId, Guid friendshipId, string content, CancellationToken cancellationToken = default);

        string BuildGroupName(Guid friendshipId);
    }
}
