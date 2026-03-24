using System.Data;
using CalendarApp.Infrastructure.Data;
using CalendarApp.Services.Meetings.Models;
using CalendarApp.Services.Messages.Models;
using Dapper;

namespace CalendarApp.Repositories.Messages;

public class DapperMessageRepository : IMessageRepository
{
    private const int AcceptedFriendshipStatusId = 2;

    private readonly IDbConnectionFactory connectionFactory;

    public DapperMessageRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<bool> HasFriendshipAccessAsync(Guid userId, Guid friendshipId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>("dbo.usp_Message_HasFriendshipAccess", new { UserId = userId, FriendshipId = friendshipId, AcceptedStatus = AcceptedFriendshipStatusId }, commandType: CommandType.StoredProcedure);
    }

    public async Task<bool> HasMeetingAccessAsync(Guid userId, Guid meetingId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>("dbo.usp_Message_HasMeetingAccess", new { UserId = userId, MeetingId = meetingId, AcceptedStatus = (int)ParticipantStatus.Accepted }, commandType: CommandType.StoredProcedure);
    }

    public async Task<(Guid Id, string? FirstName, string? LastName)?> GetSenderAsync(Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        var sender = await connection.QuerySingleOrDefaultAsync<(Guid Id, string? FirstName, string? LastName)>("dbo.usp_Message_GetSender", new { UserId = userId }, commandType: CommandType.StoredProcedure);
        return sender == default ? null : sender;
    }

    public async Task<(DateTime StartTime, string? Location)?> GetMeetingInfoAsync(Guid meetingId)
    {
        using var connection = connectionFactory.CreateConnection();
        var meeting = await connection.QuerySingleOrDefaultAsync<(Guid Id, DateTime StartTime, string? Location)>("dbo.usp_Message_GetMeetingInfo", new { MeetingId = meetingId }, commandType: CommandType.StoredProcedure);
        return meeting == default ? null : (meeting.StartTime, meeting.Location);
    }

    public async Task<Guid> InsertAsync(Guid senderId, string content, Guid? friendshipId = null, Guid? meetingId = null)
    {
        var id = Guid.NewGuid();
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync("dbo.usp_Message_Insert", new { Id = id, FriendshipId = friendshipId, MeetingId = meetingId, SenderId = senderId, Content = content, SentAt = DateTime.UtcNow }, commandType: CommandType.StoredProcedure);
        return id;
    }

    public async Task<IReadOnlyCollection<ChatMessageDto>> GetRecentFriendshipMessagesAsync(Guid friendshipId, int take)
    {
        using var connection = connectionFactory.CreateConnection();
        var messages = await connection.QueryAsync<ChatMessageDto>("dbo.usp_Message_GetRecentFriendshipMessages", new { FriendshipId = friendshipId, Take = take }, commandType: CommandType.StoredProcedure);
        return messages.OrderBy(m => m.SentAt).ToList();
    }

    public async Task<IReadOnlyCollection<ChatMessageDto>> GetRecentMeetingMessagesAsync(Guid meetingId, int take)
    {
        using var connection = connectionFactory.CreateConnection();
        var messages = await connection.QueryAsync<ChatMessageDto>("dbo.usp_Message_GetRecentMeetingMessages", new { MeetingId = meetingId, Take = take }, commandType: CommandType.StoredProcedure);
        return messages.OrderBy(m => m.SentAt).ToList();
    }
}
