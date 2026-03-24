using System.Data;
using CalendarApp.Infrastructure.Data;
using CalendarApp.Services.Friendships.Models;
using Dapper;

namespace CalendarApp.Repositories.Friendships;

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
        var rows = await connection.QueryAsync<FriendshipThreadDto>(
            "dbo.usp_Friendship_GetChatThreads",
            new { UserId = userId },
            commandType: CommandType.StoredProcedure);

        return rows.ToList();
    }

    public async Task<FriendshipThreadDto?> GetChatThreadAsync(Guid friendshipId, Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<FriendshipThreadDto>(
            "dbo.usp_Friendship_GetChatThread",
            new { FriendshipId = friendshipId, UserId = userId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<IReadOnlyCollection<FriendInfo>> GetFriendsAsync(Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<FriendInfo>(
            "dbo.usp_Friendship_GetFriends",
            new { UserId = userId },
            commandType: CommandType.StoredProcedure);

        return rows.ToList();
    }

    public async Task<IReadOnlyCollection<FriendRequestInfo>> GetPendingRequestsAsync(Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<FriendRequestInfo>(
            "dbo.usp_Friendship_GetPendingRequests",
            new { UserId = userId },
            commandType: CommandType.StoredProcedure);

        return rows.ToList();
    }

    public async Task<IReadOnlyCollection<FriendSuggestionInfo>> GetSuggestionsAsync(Guid userId, int maxSuggestions = 12)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<FriendSuggestionInfo>(
            "dbo.usp_Friendship_GetSuggestions",
            new
            {
                UserId = userId,
                MaxSuggestions = maxSuggestions
            },
            commandType: CommandType.StoredProcedure);

        return rows.ToList();
    }

    public async Task<IReadOnlyCollection<FriendSearchResultInfo>> SearchAsync(Guid userId, string term, IEnumerable<Guid> excludeIds)
    {
        var excluded = excludeIds?.Distinct().ToArray() ?? Array.Empty<Guid>();
        var excludedCsv = excluded.Length == 0 ? null : string.Join(',', excluded);

        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<FriendSearchResultInfo>(
            "dbo.usp_Friendship_Search",
            new
            {
                UserId = userId,
                SearchTerm = term.Trim().ToLowerInvariant(),
                ExcludedIds = excludedCsv
            },
            commandType: CommandType.StoredProcedure);

        return rows.ToList();
    }

    public async Task<(bool Success, Guid? FriendshipId)> SendFriendRequestAsync(Guid requesterId, Guid receiverId)
    {
        if (requesterId == receiverId)
        {
            return (false, null);
        }

        using var connection = connectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(
            "dbo.usp_Friendship_SendRequest",
            new { RequesterId = requesterId, ReceiverId = receiverId },
            commandType: CommandType.StoredProcedure);

        var result = await multi.ReadSingleAsync<(bool Success, Guid? FriendshipId)>();
        return result;
    }

    public async Task<(bool Success, Guid? RequesterId)> AcceptFriendRequestAsync(Guid friendshipId, Guid receiverId)
    {
        using var connection = connectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(
            "dbo.usp_Friendship_AcceptRequest",
            new { FriendshipId = friendshipId, ReceiverId = receiverId },
            commandType: CommandType.StoredProcedure);

        var result = await multi.ReadSingleAsync<(bool Success, Guid? RequesterId)>();
        return result;
    }

    public async Task<(bool Success, Guid? RequesterId)> DeclineFriendRequestAsync(Guid friendshipId, Guid receiverId)
    {
        using var connection = connectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(
            "dbo.usp_Friendship_DeclineRequest",
            new { FriendshipId = friendshipId, ReceiverId = receiverId },
            commandType: CommandType.StoredProcedure);

        var result = await multi.ReadSingleAsync<(bool Success, Guid? RequesterId)>();
        return result;
    }

    public async Task<(bool Success, Guid? ReceiverId)> CancelFriendRequestAsync(Guid friendshipId, Guid requesterId)
    {
        using var connection = connectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(
            "dbo.usp_Friendship_CancelRequest",
            new { FriendshipId = friendshipId, RequesterId = requesterId },
            commandType: CommandType.StoredProcedure);

        var result = await multi.ReadSingleAsync<(bool Success, Guid? ReceiverId)>();
        return result;
    }

    public async Task<bool> RemoveFriendAsync(Guid friendshipId, Guid cancelerId)
    {
        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "dbo.usp_Friendship_Remove",
            new { FriendshipId = friendshipId, CancelerId = cancelerId },
            commandType: CommandType.StoredProcedure);

        return affected > 0;
    }
}
