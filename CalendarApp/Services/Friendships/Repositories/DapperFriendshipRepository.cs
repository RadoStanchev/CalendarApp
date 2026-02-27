using CalendarApp.Data.Models;
using CalendarApp.Infrastructure.Data;
using CalendarApp.Services.Friendships.Models;
using Dapper;

namespace CalendarApp.Services.Friendships.Repositories;

public class DapperFriendshipRepository : IFriendshipRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public DapperFriendshipRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<FriendshipThreadDto>> GetChatThreadsAsync(Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<FriendshipThreadDto>(@"
SELECT f.Id AS FriendshipId,
       CASE WHEN f.RequesterId = @userId THEN f.ReceiverId ELSE f.RequesterId END AS FriendId,
       CASE WHEN f.RequesterId = @userId THEN receiver.FirstName ELSE requester.FirstName END AS FriendFirstName,
       CASE WHEN f.RequesterId = @userId THEN receiver.LastName ELSE requester.LastName END AS FriendLastName,
       CASE WHEN f.RequesterId = @userId THEN receiver.Email ELSE requester.Email END AS FriendEmail,
       f.CreatedAt,
       lastMessage.Content AS LastMessageContent,
       lastMessage.SentAt AS LastMessageSentAt
FROM dbo.Friendships f
JOIN dbo.Contacts requester ON requester.Id = f.RequesterId
JOIN dbo.Contacts receiver ON receiver.Id = f.ReceiverId
OUTER APPLY (
    SELECT TOP (1) m.Content, m.SentAt
    FROM dbo.Messages m
    WHERE m.FriendshipId = f.Id
    ORDER BY m.SentAt DESC
) lastMessage
WHERE f.Status = @acceptedStatus
  AND (f.RequesterId = @userId OR f.ReceiverId = @userId)
ORDER BY ISNULL(lastMessage.SentAt, f.CreatedAt) DESC",
            new { userId, acceptedStatus = (int)FriendshipStatus.Accepted });

        return rows.ToList();
    }

    public async Task<FriendshipThreadDto?> GetChatThreadAsync(Guid friendshipId, Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<FriendshipThreadDto>(@"
SELECT f.Id AS FriendshipId,
       CASE WHEN f.RequesterId = @userId THEN f.ReceiverId ELSE f.RequesterId END AS FriendId,
       CASE WHEN f.RequesterId = @userId THEN receiver.FirstName ELSE requester.FirstName END AS FriendFirstName,
       CASE WHEN f.RequesterId = @userId THEN receiver.LastName ELSE requester.LastName END AS FriendLastName,
       CASE WHEN f.RequesterId = @userId THEN receiver.Email ELSE requester.Email END AS FriendEmail,
       f.CreatedAt
FROM dbo.Friendships f
JOIN dbo.Contacts requester ON requester.Id = f.RequesterId
JOIN dbo.Contacts receiver ON receiver.Id = f.ReceiverId
WHERE f.Id = @friendshipId
  AND f.Status = @acceptedStatus
  AND (f.RequesterId = @userId OR f.ReceiverId = @userId)",
            new { friendshipId, userId, acceptedStatus = (int)FriendshipStatus.Accepted });
    }

    public async Task<IReadOnlyCollection<FriendInfo>> GetFriendsAsync(Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<FriendInfo>(@"
SELECT f.Id AS FriendshipId,
       CASE WHEN f.RequesterId = @userId THEN f.ReceiverId ELSE f.RequesterId END AS UserId,
       CASE WHEN f.RequesterId = @userId THEN receiver.FirstName ELSE requester.FirstName END AS FirstName,
       CASE WHEN f.RequesterId = @userId THEN receiver.LastName ELSE requester.LastName END AS LastName,
       CASE WHEN f.RequesterId = @userId THEN receiver.Email ELSE requester.Email END AS Email
FROM dbo.Friendships f
JOIN dbo.Contacts requester ON requester.Id = f.RequesterId
JOIN dbo.Contacts receiver ON receiver.Id = f.ReceiverId
WHERE f.Status = @acceptedStatus
  AND (f.RequesterId = @userId OR f.ReceiverId = @userId)
ORDER BY FirstName, LastName",
            new { userId, acceptedStatus = (int)FriendshipStatus.Accepted });

        return rows.ToList();
    }

    public async Task<IReadOnlyCollection<FriendRequestInfo>> GetPendingRequestsAsync(Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<FriendRequestInfo>(@"
SELECT f.Id AS FriendshipId,
       f.RequesterId,
       f.ReceiverId,
       CASE WHEN f.RequesterId = @userId THEN f.ReceiverId ELSE f.RequesterId END AS TargetUserId,
       CASE WHEN f.RequesterId = @userId THEN receiver.FirstName ELSE requester.FirstName END AS TargetFirstName,
       CASE WHEN f.RequesterId = @userId THEN receiver.LastName ELSE requester.LastName END AS TargetLastName,
       CASE WHEN f.RequesterId = @userId THEN receiver.Email ELSE requester.Email END AS TargetEmail,
       f.CreatedAt,
       CASE WHEN f.ReceiverId = @userId THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsIncoming
FROM dbo.Friendships f
JOIN dbo.Contacts requester ON requester.Id = f.RequesterId
JOIN dbo.Contacts receiver ON receiver.Id = f.ReceiverId
WHERE f.Status = @pendingStatus
  AND (f.RequesterId = @userId OR f.ReceiverId = @userId)
ORDER BY f.CreatedAt DESC",
            new { userId, pendingStatus = (int)FriendshipStatus.Pending });

        return rows.ToList();
    }

    public async Task<IReadOnlyCollection<FriendSuggestionInfo>> GetSuggestionsAsync(Guid userId, int maxSuggestions = 12)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<FriendSuggestionInfo>(@"
WITH MyAcceptedFriends AS (
    SELECT CASE WHEN RequesterId = @userId THEN ReceiverId ELSE RequesterId END AS FriendId
    FROM dbo.Friendships
    WHERE Status = @acceptedStatus AND (RequesterId = @userId OR ReceiverId = @userId)
)
SELECT TOP (@maxSuggestions)
    c.Id AS UserId,
    c.FirstName,
    c.LastName,
    c.Email,
    COUNT(DISTINCT fof.FriendId) AS MutualFriendCount
FROM dbo.Contacts c
LEFT JOIN (
    SELECT f2.Id,
           CASE WHEN f2.RequesterId = mf.FriendId THEN f2.ReceiverId ELSE f2.RequesterId END AS FriendId
    FROM dbo.Friendships f2
    JOIN MyAcceptedFriends mf ON (f2.RequesterId = mf.FriendId OR f2.ReceiverId = mf.FriendId)
    WHERE f2.Status = @acceptedStatus
) fof ON fof.FriendId = c.Id
WHERE c.Id <> @userId
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Friendships f
      WHERE ((f.RequesterId = @userId AND f.ReceiverId = c.Id)
         OR (f.RequesterId = c.Id AND f.ReceiverId = @userId))
        AND f.Status IN (@pendingStatus, @acceptedStatus, @blockedStatus)
  )
GROUP BY c.Id, c.FirstName, c.LastName, c.Email
ORDER BY MutualFriendCount DESC, c.FirstName, c.LastName",
            new
            {
                userId,
                maxSuggestions,
                pendingStatus = (int)FriendshipStatus.Pending,
                acceptedStatus = (int)FriendshipStatus.Accepted,
                blockedStatus = (int)FriendshipStatus.Blocked
            });

        return rows.ToList();
    }

    public async Task<IReadOnlyCollection<FriendSearchResultInfo>> SearchAsync(Guid userId, string term, IEnumerable<Guid> excludeIds)
    {
        var normalized = $"%{term.Trim().ToLower()}%";
        var excluded = excludeIds?.Distinct().ToArray() ?? Array.Empty<Guid>();
        if (excluded.Length == 0)
        {
            excluded = [Guid.Empty];
        }

        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<FriendSearchResultInfo>(@"
SELECT c.Id AS UserId,
       c.FirstName,
       c.LastName,
       c.Email,
       CASE
         WHEN f.Status = @acceptedStatus THEN @friendStatus
         WHEN f.Status = @pendingStatus AND f.ReceiverId = @userId THEN @incomingStatus
         WHEN f.Status = @pendingStatus AND f.RequesterId = @userId THEN @outgoingStatus
         WHEN f.Status = @blockedStatus THEN @blockedState
         ELSE @noneStatus
       END AS RelationshipStatus,
       f.Id AS FriendshipId,
       CASE WHEN f.Status = @pendingStatus AND f.ReceiverId = @userId THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsIncomingRequest
FROM dbo.Contacts c
OUTER APPLY (
    SELECT TOP (1) Id, RequesterId, ReceiverId, Status
    FROM dbo.Friendships
    WHERE (RequesterId = @userId AND ReceiverId = c.Id)
       OR (RequesterId = c.Id AND ReceiverId = @userId)
    ORDER BY CreatedAt DESC
) f
WHERE c.Id <> @userId
  AND c.Id NOT IN @excluded
  AND (LOWER(c.FirstName) LIKE @normalized OR LOWER(c.LastName) LIKE @normalized OR LOWER(c.Email) LIKE @normalized)
ORDER BY c.FirstName, c.LastName",
            new
            {
                userId,
                excluded,
                normalized,
                pendingStatus = (int)FriendshipStatus.Pending,
                acceptedStatus = (int)FriendshipStatus.Accepted,
                blockedStatus = (int)FriendshipStatus.Blocked,
                noneStatus = FriendRelationshipStatus.None,
                friendStatus = FriendRelationshipStatus.Friend,
                incomingStatus = FriendRelationshipStatus.IncomingRequest,
                outgoingStatus = FriendRelationshipStatus.OutgoingRequest,
                blockedState = FriendRelationshipStatus.Blocked
            });

        return rows.ToList();
    }

    public async Task<(bool Success, Guid? FriendshipId)> SendFriendRequestAsync(Guid requesterId, Guid receiverId)
    {
        if (requesterId == receiverId)
        {
            return (false, null);
        }

        using var connection = connectionFactory.CreateConnection();
        var existing = await connection.QuerySingleOrDefaultAsync<(Guid Id, int Status)>(@"
SELECT TOP (1) Id, Status
FROM dbo.Friendships
WHERE (RequesterId = @requesterId AND ReceiverId = @receiverId)
   OR (RequesterId = @receiverId AND ReceiverId = @requesterId)
ORDER BY CreatedAt DESC", new { requesterId, receiverId });

        if (existing != default && (existing.Status == (int)FriendshipStatus.Accepted || existing.Status == (int)FriendshipStatus.Pending || existing.Status == (int)FriendshipStatus.Blocked))
        {
            return (false, existing.Id);
        }

        if (existing != default)
        {
            await connection.ExecuteAsync(@"
UPDATE dbo.Friendships
SET RequesterId = @requesterId,
    ReceiverId = @receiverId,
    Status = @pendingStatus,
    CreatedAt = SYSUTCDATETIME()
WHERE Id = @id", new { requesterId, receiverId, pendingStatus = (int)FriendshipStatus.Pending, id = existing.Id });
            return (true, existing.Id);
        }

        var id = Guid.NewGuid();
        await connection.ExecuteAsync(@"
INSERT INTO dbo.Friendships (Id, RequesterId, ReceiverId, Status, CreatedAt)
VALUES (@id, @requesterId, @receiverId, @pendingStatus, SYSUTCDATETIME())",
            new { id, requesterId, receiverId, pendingStatus = (int)FriendshipStatus.Pending });

        return (true, id);
    }

    public async Task<(bool Success, Guid? RequesterId)> AcceptFriendRequestAsync(Guid friendshipId, Guid receiverId)
    {
        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
UPDATE dbo.Friendships
SET Status = @acceptedStatus,
    CreatedAt = SYSUTCDATETIME()
WHERE Id = @friendshipId
  AND Status = @pendingStatus
  AND ReceiverId = @receiverId",
            new
            {
                friendshipId,
                receiverId,
                acceptedStatus = (int)FriendshipStatus.Accepted,
                pendingStatus = (int)FriendshipStatus.Pending
            });

        if (affected == 0)
        {
            return (false, null);
        }

        var requesterId = await connection.ExecuteScalarAsync<Guid?>("SELECT RequesterId FROM dbo.Friendships WHERE Id = @friendshipId", new { friendshipId });
        return (requesterId.HasValue, requesterId);
    }

    public async Task<(bool Success, Guid? RequesterId)> DeclineFriendRequestAsync(Guid friendshipId, Guid receiverId)
    {
        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
UPDATE dbo.Friendships
SET Status = @declinedStatus,
    CreatedAt = SYSUTCDATETIME()
WHERE Id = @friendshipId
  AND Status = @pendingStatus
  AND ReceiverId = @receiverId",
            new
            {
                friendshipId,
                receiverId,
                declinedStatus = (int)FriendshipStatus.Declined,
                pendingStatus = (int)FriendshipStatus.Pending
            });

        if (affected == 0)
        {
            return (false, null);
        }

        var requesterId = await connection.ExecuteScalarAsync<Guid?>("SELECT RequesterId FROM dbo.Friendships WHERE Id = @friendshipId", new { friendshipId });
        return (requesterId.HasValue, requesterId);
    }

    public async Task<(bool Success, Guid? ReceiverId)> CancelFriendRequestAsync(Guid friendshipId, Guid requesterId)
    {
        using var connection = connectionFactory.CreateConnection();
        var receiverId = await connection.ExecuteScalarAsync<Guid?>(@"
SELECT ReceiverId
FROM dbo.Friendships
WHERE Id = @friendshipId
  AND Status = @pendingStatus
  AND RequesterId = @requesterId",
            new { friendshipId, requesterId, pendingStatus = (int)FriendshipStatus.Pending });

        if (!receiverId.HasValue)
        {
            return (false, null);
        }

        await connection.ExecuteAsync("DELETE FROM dbo.Friendships WHERE Id = @friendshipId", new { friendshipId });
        return (true, receiverId);
    }

    public async Task<bool> RemoveFriendAsync(Guid friendshipId, Guid cancelerId)
    {
        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
DELETE FROM dbo.Friendships
WHERE Id = @friendshipId
  AND Status = @acceptedStatus
  AND (RequesterId = @cancelerId OR ReceiverId = @cancelerId)",
            new { friendshipId, cancelerId, acceptedStatus = (int)FriendshipStatus.Accepted });

        return affected > 0;
    }

    public async Task<string?> GetContactFullNameAsync(Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<string>(@"
SELECT CONCAT(FirstName, ' ', LastName)
FROM dbo.Contacts
WHERE Id = @userId", new { userId });
    }
}
