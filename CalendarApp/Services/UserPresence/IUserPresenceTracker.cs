using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalendarApp.Services.UserPresence
{
    public interface IUserPresenceTracker
    {
        Task<bool> UserConnectedAsync(Guid userId, string connectionId);

        Task<bool> UserDisconnectedAsync(Guid userId, string connectionId);

        Task<bool> IsOnlineAsync(Guid userId);

        Task<IReadOnlyCollection<Guid>> GetOnlineUsersAsync();
    }
}
