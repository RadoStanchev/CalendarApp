using CalendarApp.Data.Models;
using CalendarApp.Infrastructure.Data;
using CalendarApp.Infrastructure.Data.Sql;
using CalendarApp.Services.Messages.Models;
using Dapper;

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
        return await connection.ExecuteScalarAsync<bool>(MessagesSql.HasFriendshipAccess, new { userId, friendshipId, acceptedStatus = (int)FriendshipStatus.Accepted });
    }

    public async Task<bool> HasMeetingAccessAsync(Guid userId, Guid meetingId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(MessagesSql.HasMeetingAccess, new { userId, meetingId, acceptedStatus = (int)ParticipantStatus.Accepted });
    }

    public async Task<(Guid Id, string? FirstName, string? LastName)?> GetSenderAsync(Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        var sender = await connection.QuerySingleOrDefaultAsync<(Guid Id, string? FirstName, string? LastName)>(MessagesSql.Sender, new { userId });
        return sender == default ? null : sender;
    }

    public async Task<(DateTime StartTime, string? Location)?> GetMeetingInfoAsync(Guid meetingId)
    {
        using var connection = connectionFactory.CreateConnection();
        var meeting = await connection.QuerySingleOrDefaultAsync<(Guid Id, DateTime StartTime, string? Location)>(MessagesSql.MeetingInfo, new { meetingId });
        return meeting == default ? null : (meeting.StartTime, meeting.Location);
    }

    public async Task<Guid> InsertAsync(Guid senderId, string content, Guid? friendshipId = null, Guid? meetingId = null)
    {
        var id = Guid.NewGuid();
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(MessagesSql.Insert, new { Id = id, FriendshipId = friendshipId, MeetingId = meetingId, SenderId = senderId, Content = content, SentAt = DateTime.UtcNow });
        return id;
    }

    public async Task<IReadOnlyCollection<ChatMessageDto>> GetRecentFriendshipMessagesAsync(Guid friendshipId, int take)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<MessageRow>(MessagesSql.SelectRecentByFriendship, new { friendshipId, take });
        return rows.Select(Map).OrderBy(m => m.SentAt).ToList();
    }

    public async Task<IReadOnlyCollection<ChatMessageDto>> GetRecentMeetingMessagesAsync(Guid meetingId, int take)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<MessageRow>(MessagesSql.SelectRecentByMeeting, new { meetingId, take });
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
