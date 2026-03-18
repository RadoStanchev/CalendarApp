using System.Data;
using CalendarApp.Infrastructure.Data;
using Dapper;

namespace CalendarApp.Repositories.MessageSeens;

public class DapperMessageSeenRepository : IMessageSeenRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public DapperMessageSeenRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<Guid>> GetUnseenFriendshipMessageIdsAsync(Guid userId, Guid friendshipId)
    {
        using var connection = connectionFactory.CreateConnection();
        var ids = await connection.QueryAsync<Guid>("dbo.usp_MessageSeen_GetUnseenFriendshipMessageIds", new { UserId = userId, FriendshipId = friendshipId }, commandType: CommandType.StoredProcedure);
        return ids.ToList();
    }

    public async Task<IReadOnlyCollection<Guid>> GetUnseenMeetingMessageIdsAsync(Guid userId, Guid meetingId)
    {
        using var connection = connectionFactory.CreateConnection();
        var ids = await connection.QueryAsync<Guid>("dbo.usp_MessageSeen_GetUnseenMeetingMessageIds", new { UserId = userId, MeetingId = meetingId }, commandType: CommandType.StoredProcedure);
        return ids.ToList();
    }

    public async Task InsertManyAsync(Guid userId, IEnumerable<Guid> messageIds, DateTime seenAtUtc)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var tx = connection.BeginTransaction();

        foreach (var messageId in messageIds)
        {
            await connection.ExecuteAsync(
                "dbo.usp_MessageSeen_Insert",
                new { MessageId = messageId, ContactId = userId, SeenAt = seenAtUtc },
                tx,
                commandType: CommandType.StoredProcedure);
        }

        tx.Commit();
    }
}
