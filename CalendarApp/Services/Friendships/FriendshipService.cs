using CalendarApp.Data;
using CalendarApp.Data.Models;
using CalendarApp.Services.Friendships.Models;
using CalendarApp.Services.Notifications;
using CalendarApp.Services.Notifications.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarApp.Services.Friendships
{
    public class FriendshipService : IFriendshipService
    {
        private readonly ApplicationDbContext db;
        private readonly INotificationService notificationService;

        public FriendshipService(ApplicationDbContext db, INotificationService notificationService)
        {
            this.db = db;
            this.notificationService = notificationService;
        }

        public async Task<IReadOnlyCollection<FriendshipThreadDto>> GetChatThreadsAsync(Guid userId)
        {
            var friendships = await db.Friendships
                .AsNoTracking()
                .Where(f => f.Status == FriendshipStatus.Accepted
                            && ((f.RequesterId.HasValue && f.RequesterId.Value == userId)
                                || (f.ReceiverId.HasValue && f.ReceiverId.Value == userId)))
                .Where(f => f.RequesterId.HasValue && f.ReceiverId.HasValue)
                .Select(f => new
                {
                    f.Id,
                    f.CreatedAt,
                    FriendId = f.RequesterId == userId ? f.ReceiverId.Value : f.RequesterId.Value,
                    FriendFirstName = f.RequesterId == userId ? (f.Receiver != null ? f.Receiver.FirstName : null) : (f.Requester != null ? f.Requester.FirstName : null),
                    FriendLastName = f.RequesterId == userId ? (f.Receiver != null ? f.Receiver.LastName : null) : (f.Requester != null ? f.Requester.LastName : null),
                    FriendEmail = f.RequesterId == userId ? (f.Receiver != null ? f.Receiver.Email : null) : (f.Requester != null ? f.Requester.Email : null)
                })
                .ToListAsync();

            if (friendships.Count == 0)
            {
                return Array.Empty<FriendshipThreadDto>();
            }

            var friendshipIds = friendships.Select(f => f.Id).ToList();

            var latestMessages = await db.Messages
                .AsNoTracking()
                .Where(m => m.FriendshipId != null && friendshipIds.Contains(m.FriendshipId.Value))
                .OrderByDescending(m => m.SentAt)
                .Select(m => new
                {
                    FriendshipId = m.FriendshipId.Value,
                    m.Content,
                    m.SentAt
                })
                .ToListAsync();

            var latestMessageLookup = latestMessages
                .GroupBy(m => m.FriendshipId)
                .ToDictionary(g => g.Key, g => g.First());

            return friendships
                .Select(f =>
                {
                    latestMessageLookup.TryGetValue(f.Id, out var lastMessage);

                    return new FriendshipThreadDto
                    {
                        FriendshipId = f.Id,
                        FriendId = f.FriendId,
                        FriendFirstName = f.FriendFirstName,
                        FriendLastName = f.FriendLastName,
                        FriendEmail = f.FriendEmail,
                        CreatedAt = f.CreatedAt,
                        LastMessageContent = lastMessage?.Content,
                        LastMessageSentAt = lastMessage?.SentAt
                    };
                })
                .ToList();
        }

        public async Task<FriendshipThreadDto?> GetChatThreadAsync(Guid friendshipId, Guid userId)
        {
            return await db.Friendships
                .AsNoTracking()
                .Where(f => f.Id == friendshipId
                            && f.Status == FriendshipStatus.Accepted
                            && ((f.RequesterId.HasValue && f.RequesterId.Value == userId)
                                || (f.ReceiverId.HasValue && f.ReceiverId.Value == userId)))
                .Where(f => f.RequesterId.HasValue && f.ReceiverId.HasValue)
                .Select(f => new FriendshipThreadDto
                {
                    FriendshipId = f.Id,
                    FriendId = f.RequesterId == userId ? f.ReceiverId.Value : f.RequesterId.Value,
                    FriendFirstName = f.RequesterId == userId ? (f.Receiver != null ? f.Receiver.FirstName : null) : (f.Requester != null ? f.Requester.FirstName : null),
                    FriendLastName = f.RequesterId == userId ? (f.Receiver != null ? f.Receiver.LastName : null) : (f.Requester != null ? f.Requester.LastName : null),
                    FriendEmail = f.RequesterId == userId ? (f.Receiver != null ? f.Receiver.Email : null) : (f.Requester != null ? f.Requester.Email : null),
                    CreatedAt = f.CreatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyCollection<FriendInfo>> GetFriendsAsync(Guid userId)
        {
            return await db.Friendships
                .AsNoTracking()
                .Where(f => f.Status == FriendshipStatus.Accepted
                            && ((f.RequesterId.HasValue && f.RequesterId.Value == userId)
                                || (f.ReceiverId.HasValue && f.ReceiverId.Value == userId)))
                .Where(f => f.RequesterId.HasValue && f.ReceiverId.HasValue)
                .Select(f => new FriendInfo
                {
                    FriendshipId = f.Id,
                    UserId = f.RequesterId == userId ? f.ReceiverId.Value : f.RequesterId.Value,
                    FirstName = f.RequesterId == userId ? (f.Receiver != null ? f.Receiver.FirstName : string.Empty) : (f.Requester != null ? f.Requester.FirstName : string.Empty),
                    LastName = f.RequesterId == userId ? (f.Receiver != null ? f.Receiver.LastName : string.Empty) : (f.Requester != null ? f.Requester.LastName : string.Empty),
                    Email = f.RequesterId == userId ? (f.Receiver != null ? f.Receiver.Email : string.Empty) : (f.Requester != null ? f.Requester.Email : string.Empty)
                })
                .OrderBy(f => f.FirstName)
                .ThenBy(f => f.LastName)
                .ToListAsync();
        }

        public async Task<IReadOnlyCollection<FriendRequestInfo>> GetPendingRequestsAsync(Guid userId)
        {
            return await db.Friendships
                .AsNoTracking()
                .Where(f => f.Status == FriendshipStatus.Pending
                            && ((f.RequesterId.HasValue && f.RequesterId.Value == userId)
                                || (f.ReceiverId.HasValue && f.ReceiverId.Value == userId)))
                .Where(f => f.RequesterId.HasValue && f.ReceiverId.HasValue)
                .Select(f => new FriendRequestInfo
                {
                    FriendshipId = f.Id,
                    RequesterId = f.RequesterId.Value,
                    ReceiverId = f.ReceiverId.Value,
                    TargetUserId = f.RequesterId == userId ? f.ReceiverId.Value : f.RequesterId.Value,
                    TargetFirstName = f.RequesterId == userId ? (f.Receiver != null ? f.Receiver.FirstName : string.Empty) : (f.Requester != null ? f.Requester.FirstName : string.Empty),
                    TargetLastName = f.RequesterId == userId ? (f.Receiver != null ? f.Receiver.LastName : string.Empty) : (f.Requester != null ? f.Requester.LastName : string.Empty),
                    TargetEmail = f.RequesterId == userId ? (f.Receiver != null ? f.Receiver.Email : string.Empty) : (f.Requester != null ? f.Requester.Email : string.Empty),
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
                .Where(f => (f.RequesterId.HasValue && f.RequesterId.Value == userId)
                            || (f.ReceiverId.HasValue && f.ReceiverId.Value == userId))
                .Select(f => new
                {
                    f.RequesterId,
                    f.ReceiverId,
                    f.Status
                })
                .ToListAsync();

            var acceptedFriends = new HashSet<Guid>(userFriendships
                .Where(f => f.Status == FriendshipStatus.Accepted)
                .Select(f => f.RequesterId == userId ? f.ReceiverId : f.RequesterId)
                .Where(id => id.HasValue)
                .Select(id => id.Value));

            var excluded = new HashSet<Guid> { userId };
            foreach (var friendship in userFriendships)
            {
                var otherId = friendship.RequesterId == userId ? friendship.ReceiverId : friendship.RequesterId;
                if (otherId.HasValue)
                {
                    excluded.Add(otherId.Value);
                }
            }

            var mutualCounts = new Dictionary<Guid, int>();

            if (acceptedFriends.Count > 0)
            {
                var friendNetworks = await db.Friendships
                    .AsNoTracking()
                    .Where(f => f.Status == FriendshipStatus.Accepted
                                && f.RequesterId.HasValue
                                && f.ReceiverId.HasValue
                                && (acceptedFriends.Contains(f.RequesterId.Value) || acceptedFriends.Contains(f.ReceiverId.Value)))
                    .Select(f => new { f.RequesterId, f.ReceiverId })
                    .ToListAsync();

                foreach (var friendship in friendNetworks)
                {
                    if (friendship.RequesterId.HasValue && acceptedFriends.Contains(friendship.RequesterId.Value))
                    {
                        var candidateId = friendship.ReceiverId.Value;
                        if (!excluded.Contains(candidateId))
                        {
                            mutualCounts[candidateId] = mutualCounts.TryGetValue(candidateId, out var count) ? count + 1 : 1;
                        }
                    }

                    if (friendship.ReceiverId.HasValue && acceptedFriends.Contains(friendship.ReceiverId.Value))
                    {
                        var candidateId = friendship.RequesterId.Value;
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

        public async Task<IReadOnlyCollection<FriendSearchResultInfo>> SearchAsync(Guid userId, string term, IEnumerable<Guid> excludeIds)
        {
            term = term?.Trim() ?? string.Empty;
            if (term.Length < 2)
            {
                return Array.Empty<FriendSearchResultInfo>();
            }

            var exclude = new HashSet<Guid>(excludeIds ?? Enumerable.Empty<Guid>()) { userId };

            var relationshipStatuses = await db.Friendships
                .AsNoTracking()
                .Where(f => (f.RequesterId.HasValue && f.RequesterId.Value == userId)
                            || (f.ReceiverId.HasValue && f.ReceiverId.Value == userId))
                .Select(f => new
                {
                    f.Id,
                    f.RequesterId,
                    f.ReceiverId,
                    f.Status
                })
                .ToListAsync();

            var statusLookup = new Dictionary<Guid, (FriendRelationshipStatus Status, Guid FriendshipId, bool IsIncoming)>();
            foreach (var relationship in relationshipStatuses)
            {
                var otherUserId = relationship.RequesterId == userId ? relationship.ReceiverId : relationship.RequesterId;
                if (!otherUserId.HasValue)
                {
                    continue;
                }

                var status = relationship.Status switch
                {
                    FriendshipStatus.Accepted => FriendRelationshipStatus.Friend,
                    FriendshipStatus.Pending when relationship.RequesterId == userId => FriendRelationshipStatus.OutgoingRequest,
                    FriendshipStatus.Pending when relationship.ReceiverId == userId => FriendRelationshipStatus.IncomingRequest,
                    FriendshipStatus.Blocked => FriendRelationshipStatus.Blocked,
                    _ => FriendRelationshipStatus.None
                };

                statusLookup[otherUserId.Value] = (status, relationship.Id, relationship.ReceiverId == userId);
            }

            var query = db.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(term))
            {
                var pattern = $"%{term}%";
                query = query.Where(u => EF.Functions.Like(u.FirstName ?? string.Empty, pattern)
                    || EF.Functions.Like(u.LastName ?? string.Empty, pattern)
                    || EF.Functions.Like(u.Email ?? string.Empty, pattern));
            }

            var matches = await query
                .Where(u => !exclude.Contains(u.Id))
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Take(10)
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Email
                })
                .ToListAsync();

            return matches
                .Select(match =>
                {
                    var (status, friendshipId, isIncoming) = statusLookup.TryGetValue(match.Id, out var info)
                        ? info
                        : (FriendRelationshipStatus.None, Guid.Empty, false);

                    return new FriendSearchResultInfo
                    {
                        UserId = match.Id,
                        FirstName = match.FirstName ?? string.Empty,
                        LastName = match.LastName ?? string.Empty,
                        Email = match.Email ?? string.Empty,
                        RelationshipStatus = status,
                        FriendshipId = status == FriendRelationshipStatus.None ? null : friendshipId,
                        IsIncomingRequest = status == FriendRelationshipStatus.IncomingRequest && isIncoming
                    };
                })
                .ToList();
        }

        public async Task<(bool Success, Guid? FriendshipId)> SendFriendRequestAsync(Guid requesterId, Guid receiverId)
        {
            if (requesterId == receiverId)
            {
                return (false, null);
            }

            var friendship = await db.Friendships
                .FirstOrDefaultAsync(f =>
                    (f.RequesterId == requesterId && f.ReceiverId == receiverId) ||
                    (f.RequesterId == receiverId && f.ReceiverId == requesterId));

            if (friendship != null)
            {
                if (friendship.Status == FriendshipStatus.Accepted)
                {
                    return (false, friendship.Id);
                }

                if (friendship.Status == FriendshipStatus.Pending)
                {
                    return (false, friendship.Id);
                }

                if (friendship.Status == FriendshipStatus.Blocked)
                {
                    return (false, friendship.Id);
                }

                friendship.RequesterId = requesterId;
                friendship.ReceiverId = receiverId;
                friendship.Status = FriendshipStatus.Pending;
                friendship.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                friendship = new Friendship
                {
                    RequesterId = requesterId,
                    ReceiverId = receiverId,
                    Status = FriendshipStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                await db.Friendships.AddAsync(friendship);
            }

            await db.SaveChangesAsync();

            var requesterName = await GetUserDisplayNameAsync(requesterId);
            await notificationService.CreateNotificationAsync(new NotificationCreateDto
            {
                UserId = receiverId,
                Message = $"{requesterName} ви изпрати покана за приятелство.",
                Type = NotificationType.Invitation
            });

            return (true, friendship.Id);
        }

        public async Task<bool> AcceptFriendRequestAsync(Guid friendshipId, Guid receiverId)
        {
            var friendship = await db.Friendships.FirstOrDefaultAsync(f => f.Id == friendshipId);
            if (friendship == null || friendship.Status != FriendshipStatus.Pending || friendship.ReceiverId != receiverId)
            {
                return false;
            }

            if (!friendship.RequesterId.HasValue)
            {
                db.Friendships.Remove(friendship);
                await db.SaveChangesAsync();
                return true;
            }

            friendship.Status = FriendshipStatus.Accepted;
            friendship.CreatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var receiverName = await GetUserDisplayNameAsync(receiverId);
            await notificationService.CreateNotificationAsync(new NotificationCreateDto
            {
                UserId = friendship.RequesterId.Value,
                Message = $"{receiverName} прие вашата покана за приятелство.",
                Type = NotificationType.Info
            });

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

            if (friendship.RequesterId.HasValue)
            {
                var receiverName = await GetUserDisplayNameAsync(receiverId);
                await notificationService.CreateNotificationAsync(new NotificationCreateDto
                {
                    UserId = friendship.RequesterId.Value,
                    Message = $"{receiverName} отказа вашата покана за приятелство.",
                    Type = NotificationType.Warning
                });
            }

            return true;
        }

        public async Task<bool> CancelFriendRequestAsync(Guid friendshipId, Guid requesterId)
        {
            var friendship = await db.Friendships.FirstOrDefaultAsync(f => f.Id == friendshipId);
            if (friendship == null || friendship.Status != FriendshipStatus.Pending || friendship.RequesterId != requesterId)
            {
                return false;
            }

            var receiverId = friendship.ReceiverId;
            db.Friendships.Remove(friendship);
            await db.SaveChangesAsync();

            var requesterName = await GetUserDisplayNameAsync(requesterId);
            if (receiverId.HasValue)
            {
                await notificationService.CreateNotificationAsync(new NotificationCreateDto
                {
                    UserId = receiverId.Value,
                    Message = $"{requesterName} отмени поканата за приятелство.",
                    Type = NotificationType.Info
                });
            }

            return true;
        }

        public async Task<bool> RemoveFriendAsync(Guid friendshipId, Guid cancelerId)
        {
            var friendship = await db.Friendships
                .FirstOrDefaultAsync(f => f.Id == friendshipId);

            if (friendship == null || friendship.Status != FriendshipStatus.Accepted)
            {
                return false;
            }

            var requesterId = friendship.RequesterId;
            var receiverId = friendship.ReceiverId;

            var isCancelerParticipant =
                (requesterId.HasValue && requesterId.Value == cancelerId) ||
                (receiverId.HasValue && receiverId.Value == cancelerId);

            if (!isCancelerParticipant && (requesterId.HasValue || receiverId.HasValue))
            {
                return false;
            }

            db.Friendships.Remove(friendship);
            await db.SaveChangesAsync();
            return true;
        }

        private async Task<string> GetUserDisplayNameAsync(Guid userId)
        {
            var user = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.FirstName, u.LastName })
                .FirstOrDefaultAsync();

            return FormatName(user?.FirstName, user?.LastName);
        }

        private static string FormatName(string? firstName, string? lastName)
        {
            var parts = new[] { firstName, lastName }
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();

            return parts.Length > 0 ? string.Join(" ", parts) : "Неизвестен";
        }

    }
}
