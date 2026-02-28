using CalendarApp.Infrastructure.Data;
using CalendarApp.Services.Messages.Models;
using Dapper;
using CalendarApp.Services.Friendships.Models;
using CalendarApp.Services.Meetings.Models;

namespace CalendarApp.Services.Messages.Repositories;

public class DapperMessageRepository : IMessageRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public DapperMessageRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<bool> HasFriendshipAccessAsync(Guid userId, Guid friendshipId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(@"SELECT CAST(CASE WHEN EXISTS (
SELECT 1 FROM dbo.Friendships
WHERE Id = @friendshipId
  AND Status = @acceptedStatus
  AND (RequesterId = @userId OR ReceiverId = @userId)) THEN 1 ELSE 0 END AS bit)", new { userId, friendshipId, acceptedStatus = (int)FriendshipStatus.Accepted });
    }

    public async Task<bool> HasMeetingAccessAsync(Guid userId, Guid meetingId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(@"SELECT CAST(CASE WHEN EXISTS (
SELECT 1 FROM dbo.Meetings m
WHERE m.Id = @meetingId
AND (
    m.CreatedById = @userId OR EXISTS (
        SELECT 1 FROM dbo.MeetingParticipants mp
        WHERE mp.MeetingId = m.Id AND mp.ContactId = @userId AND mp.Status = @acceptedStatus
    )
)) THEN 1 ELSE 0 END AS bit)", new { userId, meetingId, acceptedStatus = (int)ParticipantStatus.Accepted });
    }

    public async Task<(Guid Id, string? FirstName, string? LastName)?> GetSenderAsync(Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        var sender = await connection.QuerySingleOrDefaultAsync<(Guid Id, string? FirstName, string? LastName)>("SELECT TOP 1 Id, FirstName, LastName FROM dbo.Contacts WHERE Id = @userId", new { userId });
        return sender == default ? null : sender;
    }

    public async Task<(DateTime StartTime, string? Location)?> GetMeetingInfoAsync(Guid meetingId)
    {
        using var connection = connectionFactory.CreateConnection();
        var meeting = await connection.QuerySingleOrDefaultAsync<(Guid Id, DateTime StartTime, string? Location)>("SELECT TOP 1 Id, StartTime, Location FROM dbo.Meetings WHERE Id = @meetingId", new { meetingId });
        return meeting == default ? null : (meeting.StartTime, meeting.Location);
    }

    public async Task<Guid> InsertAsync(Guid senderId, string content, Guid? friendshipId = null, Guid? meetingId = null)
    {
        var id = Guid.NewGuid();
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"INSERT INTO dbo.Messages (Id, FriendshipId, MeetingId, SenderId, Content, SentAt)
VALUES (@Id, @FriendshipId, @MeetingId, @SenderId, @Content, @SentAt)", new { Id = id, FriendshipId = friendshipId, MeetingId = meetingId, SenderId = senderId, Content = content, SentAt = DateTime.UtcNow });
        return id;
    }

    public async Task<IReadOnlyCollection<ChatMessageDto>> GetRecentFriendshipMessagesAsync(Guid friendshipId, int take)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<MessageRow>(@"SELECT TOP (@take)
m.Id, m.SenderId, m.Content, m.SentAt, m.FriendshipId, m.MeetingId,
c.FirstName AS SenderFirstName, c.LastName AS SenderLastName
FROM dbo.Messages m
INNER JOIN dbo.Contacts c ON c.Id = m.SenderId
WHERE m.FriendshipId = @friendshipId
ORDER BY m.SentAt DESC", new { friendshipId, take });
        return rows.Select(Map).OrderBy(m => m.SentAt).ToList();
    }

    public async Task<IReadOnlyCollection<ChatMessageDto>> GetRecentMeetingMessagesAsync(Guid meetingId, int take)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<MessageRow>(@"SELECT TOP (@take)
m.Id, m.SenderId, m.Content, m.SentAt, m.FriendshipId, m.MeetingId,
c.FirstName AS SenderFirstName, c.LastName AS SenderLastName
FROM dbo.Messages m
INNER JOIN dbo.Contacts c ON c.Id = m.SenderId
WHERE m.MeetingId = @meetingId
ORDER BY m.SentAt DESC", new { meetingId, take });
        return rows.Select(Map).OrderBy(m => m.SentAt).ToList();
    }

    private static ChatMessageDto Map(MessageRow row) => new()
    {
        FriendshipId = row.FriendshipId,
        MeetingId = row.MeetingId,
        MessageId = row.Id,
        SenderId = row.SenderId,
        SenderName = string.Join(" ", new[] { row.SenderFirstName, row.SenderLastName }.Where(x => !string.IsNullOrWhiteSpace(x))),
        Content = row.Content,
        SentAt = row.SentAt,
        Metadata = new Dictionary<string, string?>()
    };

    private sealed class MessageRow
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public Guid? FriendshipId { get; set; }
        public Guid? MeetingId { get; set; }
        public string? SenderFirstName { get; set; }
        public string? SenderLastName { get; set; }
    }

}
