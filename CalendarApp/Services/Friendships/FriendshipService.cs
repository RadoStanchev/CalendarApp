using CalendarApp.Data;
using CalendarApp.Data.Models;
using CalendarApp.Services.Friendships.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace CalendarApp.Services.Friendships
{
    public class FriendshipService : IFriendshipService
    {
        private readonly ApplicationDbContext db;

        public FriendshipService(ApplicationDbContext db)
        {
            this.db = db;
        }

        public async Task<IReadOnlyCollection<FriendInfo>> GetFriendsAsync(Guid userId)
        {
            return await db.Friendships
                .AsNoTracking()
                .Where(f => f.Status == FriendshipStatus.Accepted && (f.RequesterId == userId || f.ReceiverId == userId))
                .Select(f => new FriendInfo
                {
                    UserId = f.RequesterId == userId ? f.Receiver.Id : f.Requester.Id,
                    FirstName = f.RequesterId == userId ? f.Receiver.FirstName : f.Requester.FirstName,
                    LastName = f.RequesterId == userId ? f.Receiver.LastName : f.Requester.LastName,
                    Email = f.RequesterId == userId ? f.Receiver.Email : f.Requester.Email
                })
                .OrderBy(f => f.FirstName)
                .ThenBy(f => f.LastName)
                .ToListAsync();
        }

        public async Task<IReadOnlyCollection<FriendRequestInfo>> GetPendingRequestsAsync(Guid userId)
        {
            return await db.Friendships
                .AsNoTracking()
                .Where(f => f.Status == FriendshipStatus.Pending && (f.RequesterId == userId || f.ReceiverId == userId))
                .Select(f => new FriendRequestInfo
                {
                    FriendshipId = f.Id,
                    RequesterId = f.RequesterId,
                    ReceiverId = f.ReceiverId,
                    TargetUserId = f.RequesterId == userId ? f.Receiver.Id : f.Requester.Id,
                    TargetFirstName = f.RequesterId == userId ? f.Receiver.FirstName : f.Requester.FirstName,
                    TargetLastName = f.RequesterId == userId ? f.Receiver.LastName : f.Requester.LastName,
                    TargetEmail = f.RequesterId == userId ? f.Receiver.Email : f.Requester.Email,
                    CreatedAt = f.CreatedAt,
                    IsIncoming = f.ReceiverId == userId
                })
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<IReadOnlyCollection<FriendSuggestionInfo>> GetSuggestionsAsync(Guid userId, int maxSuggestions = 12)
        {
            var userFriendships = await db.Friendships
                .AsNoTracking()
                .Where(f => f.RequesterId == userId || f.ReceiverId == userId)
                .Select(f => new
                {
                    f.RequesterId,
                    f.ReceiverId,
                    f.Status
                })
                .ToListAsync();

            var acceptedFriends = new HashSet<Guid>(userFriendships
                .Where(f => f.Status == FriendshipStatus.Accepted)
                .Select(f => f.RequesterId == userId ? f.ReceiverId : f.RequesterId));

            var excluded = new HashSet<Guid> { userId };
            foreach (var friendship in userFriendships)
            {
                excluded.Add(friendship.RequesterId == userId ? friendship.ReceiverId : friendship.RequesterId);
            }

            var mutualCounts = new Dictionary<Guid, int>();

            if (acceptedFriends.Count > 0)
            {
                var friendNetworks = await db.Friendships
                    .AsNoTracking()
                    .Where(f => f.Status == FriendshipStatus.Accepted && (acceptedFriends.Contains(f.RequesterId) || acceptedFriends.Contains(f.ReceiverId)))
                    .Select(f => new { f.RequesterId, f.ReceiverId })
                    .ToListAsync();

                foreach (var friendship in friendNetworks)
                {
                    if (acceptedFriends.Contains(friendship.RequesterId))
                    {
                        var candidateId = friendship.ReceiverId;
                        if (!excluded.Contains(candidateId))
                        {
                            mutualCounts[candidateId] = mutualCounts.TryGetValue(candidateId, out var count) ? count + 1 : 1;
                        }
                    }

                    if (acceptedFriends.Contains(friendship.ReceiverId))
                    {
                        var candidateId = friendship.RequesterId;
                        if (!excluded.Contains(candidateId))
                        {
                            mutualCounts[candidateId] = mutualCounts.TryGetValue(candidateId, out var count) ? count + 1 : 1;
                        }
                    }
                }
            }

            var candidateIds = mutualCounts
                .OrderByDescending(x => x.Value)
                .ThenBy(x => x.Key)
                .Take(maxSuggestions)
                .Select(x => x.Key)
                .ToList();

            var suggestions = new List<FriendSuggestionInfo>();

            if (candidateIds.Count > 0)
            {
                var candidates = await db.Users
                    .AsNoTracking()
                    .Where(u => candidateIds.Contains(u.Id))
                    .Select(u => new
                    {
                        u.Id,
                        u.FirstName,
                        u.LastName,
                        u.Email
                    })
                    .ToListAsync();

                var orderMap = mutualCounts;
                suggestions.AddRange(candidates.Select(candidate => new FriendSuggestionInfo
                {
                    UserId = candidate.Id,
                    FirstName = candidate.FirstName,
                    LastName = candidate.LastName,
                    Email = candidate.Email,
                    MutualFriendCount = orderMap.TryGetValue(candidate.Id, out var count) ? count : 0
                })
                .OrderByDescending(c => c.MutualFriendCount)
                .ThenBy(c => c.FirstName)
                .ThenBy(c => c.LastName));
            }

            if (suggestions.Count < maxSuggestions)
            {
                var fallback = await db.Users
                    .AsNoTracking()
                    .Where(u => !excluded.Contains(u.Id))
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .Take(maxSuggestions - suggestions.Count)
                    .Select(u => new FriendSuggestionInfo
                    {
                        UserId = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email,
                        MutualFriendCount = 0
                    })
                    .ToListAsync();

                foreach (var candidate in fallback)
                {
                    if (suggestions.All(s => s.UserId != candidate.UserId))
                    {
                        suggestions.Add(candidate);
                    }
                }
            }

            return suggestions
                .OrderByDescending(s => s.MutualFriendCount)
                .ThenBy(s => s.FirstName)
                .ThenBy(s => s.LastName)
                .ToList();
        }

        public async Task<bool> SendFriendRequestAsync(Guid requesterId, Guid receiverId)
        {
            if (requesterId == receiverId)
            {
                return false;
            }

            var pairKey = BuildPairKey(requesterId, receiverId);

            var friendship = await db.Friendships
                .FirstOrDefaultAsync(f => f.PairKey == pairKey);

            if (friendship != null)
            {
                if (friendship.Status == FriendshipStatus.Accepted)
                {
                    return false;
                }

                if (friendship.Status == FriendshipStatus.Pending)
                {
                    return false;
                }

                if (friendship.Status == FriendshipStatus.Blocked)
                {
                    return false;
                }

                friendship.RequesterId = requesterId;
                friendship.ReceiverId = receiverId;
                friendship.Status = FriendshipStatus.Pending;
                friendship.CreatedAt = DateTime.UtcNow;
                friendship.PairKey = pairKey;
            }
            else
            {
                friendship = new Friendship
                {
                    RequesterId = requesterId,
                    ReceiverId = receiverId,
                    Status = FriendshipStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    PairKey = pairKey
                };

                await db.Friendships.AddAsync(friendship);
            }

            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AcceptFriendRequestAsync(Guid friendshipId, Guid receiverId)
        {
            var friendship = await db.Friendships.FirstOrDefaultAsync(f => f.Id == friendshipId);
            if (friendship == null || friendship.Status != FriendshipStatus.Pending || friendship.ReceiverId != receiverId)
            {
                return false;
            }

            friendship.Status = FriendshipStatus.Accepted;
            friendship.CreatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeclineFriendRequestAsync(Guid friendshipId, Guid receiverId)
        {
            var friendship = await db.Friendships.FirstOrDefaultAsync(f => f.Id == friendshipId);
            if (friendship == null || friendship.Status != FriendshipStatus.Pending || friendship.ReceiverId != receiverId)
            {
                return false;
            }

            friendship.Status = FriendshipStatus.Declined;
            friendship.CreatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelFriendRequestAsync(Guid friendshipId, Guid requesterId)
        {
            var friendship = await db.Friendships.FirstOrDefaultAsync(f => f.Id == friendshipId);
            if (friendship == null || friendship.Status != FriendshipStatus.Pending || friendship.RequesterId != requesterId)
            {
                return false;
            }

            db.Friendships.Remove(friendship);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveFriendAsync(Guid userId, Guid friendId)
        {
            var pairKey = BuildPairKey(userId, friendId);

            var friendship = await db.Friendships
                .FirstOrDefaultAsync(f => f.PairKey == pairKey && f.Status == FriendshipStatus.Accepted);

            if (friendship == null)
            {
                return false;
            }

            db.Friendships.Remove(friendship);
            await db.SaveChangesAsync();
            return true;
        }

        private static string BuildPairKey(Guid first, Guid second)
        {
            return first.CompareTo(second) < 0
                ? $"{first:D}_{second:D}"
                : $"{second:D}_{first:D}";
        }
    }
}
