using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarApp.Services.UserPresence
{
    public class UserPresenceTracker : IUserPresenceTracker
    {
        private readonly ConcurrentDictionary<Guid, HashSet<string>> connections = new();

        public Task<bool> UserConnectedAsync(Guid userId, string connectionId)
        {
            if (userId == Guid.Empty || string.IsNullOrWhiteSpace(connectionId))
            {
                return Task.FromResult(false);
            }

            var connectionSet = connections.GetOrAdd(userId, _ => new HashSet<string>());

            lock (connectionSet)
            {
                var wasOnline = connectionSet.Count > 0;
                connectionSet.Add(connectionId);
                return Task.FromResult(!wasOnline && connectionSet.Count > 0);
            }
        }

        public Task<bool> UserDisconnectedAsync(Guid userId, string connectionId)
        {
            if (userId == Guid.Empty || string.IsNullOrWhiteSpace(connectionId))
            {
                return Task.FromResult(false);
            }

            if (!connections.TryGetValue(userId, out var connectionSet))
            {
                return Task.FromResult(false);
            }

            lock (connectionSet)
            {
                if (!connectionSet.Remove(connectionId))
                {
                    return Task.FromResult(false);
                }

                if (connectionSet.Count == 0)
                {
                    connections.TryRemove(userId, out _);
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }
        }

        public Task<bool> IsOnlineAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(connections.TryGetValue(userId, out var connectionSet) && connectionSet.Count > 0);
        }

        public Task<IReadOnlyCollection<Guid>> GetOnlineUsersAsync()
        {
            var snapshot = connections
                .Where(pair => pair.Value.Count > 0)
                .Select(pair => pair.Key)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<Guid>>(snapshot);
        }
    }
}
