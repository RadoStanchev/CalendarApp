using CalendarApp.Infrastructure.Data;
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
        var ids = await connection.QueryAsync<Guid>(@"SELECT m.Id
FROM dbo.Messages m
WHERE m.FriendshipId = @friendshipId
  AND m.SenderId <> @userId
  AND NOT EXISTS (
      SELECT 1 FROM dbo.MessageSeens ms
      WHERE ms.MessageId = m.Id AND ms.ContactId = @userId
  )", new { userId, friendshipId });
        return ids.ToList();
    }

    public async Task<IReadOnlyCollection<Guid>> GetUnseenMeetingMessageIdsAsync(Guid userId, Guid meetingId)
    {
        using var connection = connectionFactory.CreateConnection();
        var ids = await connection.QueryAsync<Guid>(@"SELECT m.Id
FROM dbo.Messages m
WHERE m.MeetingId = @meetingId
  AND m.SenderId <> @userId
  AND NOT EXISTS (
      SELECT 1 FROM dbo.MessageSeens ms
      WHERE ms.MessageId = m.Id AND ms.ContactId = @userId
  )", new { userId, meetingId });
        return ids.ToList();
    }

    public async Task InsertManyAsync(Guid userId, IEnumerable<Guid> messageIds, DateTime seenAtUtc)
    {
        using var connection = connectionFactory.CreateConnection();
        using var tx = connection.BeginTransaction();
        foreach (var messageId in messageIds)
        {
            await connection.ExecuteAsync(@"INSERT INTO dbo.MessageSeens (MessageId, ContactId, SeenAt)
VALUES (@MessageId, @ContactId, @SeenAt)", new { MessageId = messageId, ContactId = userId, SeenAt = seenAtUtc }, tx);
        }

        tx.Commit();
    }

}
