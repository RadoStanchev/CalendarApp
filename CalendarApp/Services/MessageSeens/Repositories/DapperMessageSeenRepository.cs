using CalendarApp.Infrastructure.Data;
using CalendarApp.Infrastructure.Data.Sql;
using Dapper;

namespace CalendarApp.Services.MessageSeens.Repositories;

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
        var ids = await connection.QueryAsync<Guid>(MessageSeensSql.UnseenByFriendship, new { userId, friendshipId });
        return ids.ToList();
    }

    public async Task<IReadOnlyCollection<Guid>> GetUnseenMeetingMessageIdsAsync(Guid userId, Guid meetingId)
    {
        using var connection = connectionFactory.CreateConnection();
        var ids = await connection.QueryAsync<Guid>(MessageSeensSql.UnseenByMeeting, new { userId, meetingId });
        return ids.ToList();
    }

    public async Task InsertManyAsync(Guid userId, IEnumerable<Guid> messageIds, DateTime seenAtUtc)
    {
        using var connection = connectionFactory.CreateConnection();
        using var tx = connection.BeginTransaction();
        foreach (var messageId in messageIds)
        {
            await connection.ExecuteAsync(MessageSeensSql.Insert, new { MessageId = messageId, ContactId = userId, SeenAt = seenAtUtc }, tx);
        }

        tx.Commit();
    }
}
