using System.Data;
using CalendarApp.Infrastructure.Data;
using CalendarApp.Models.Meetings;
using CalendarApp.Services.Meetings.Models;
using Dapper;

namespace CalendarApp.Services.Meetings.Repositories;

public class DapperMeetingRepository : IMeetingRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public DapperMeetingRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<MeetingRecord?> GetByIdAsync(Guid meetingId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<MeetingRecord>(@"SELECT TOP (1) Id, StartTime, Location, Description, CategoryId, CreatedById, ReminderSent
FROM dbo.Meetings
WHERE Id = @meetingId", new { meetingId });
    }

    public async Task<IReadOnlyCollection<MeetingThreadDto>> GetChatThreadsAsync(Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<MeetingThreadDto>(@"
SELECT m.Id AS MeetingId,
       m.Description,
       m.StartTime,
       m.Location,
       m.CreatedById,
       creator.FirstName AS CreatorFirstName,
       creator.LastName AS CreatorLastName,
       (SELECT COUNT(*) FROM dbo.MeetingParticipants mp WHERE mp.MeetingId = m.Id AND mp.Status = @acceptedParticipantStatus) AS ParticipantCount,
       lastMessage.Content AS LastMessageContent,
       lastMessage.SentAt AS LastMessageSentAt
FROM dbo.Meetings m
JOIN dbo.Contacts creator ON creator.Id = m.CreatedById
OUTER APPLY (
    SELECT TOP (1) Content, SentAt
    FROM dbo.Messages msg
    WHERE msg.MeetingId = m.Id
    ORDER BY msg.SentAt DESC
) lastMessage
WHERE m.CreatedById = @userId
   OR EXISTS (SELECT 1 FROM dbo.MeetingParticipants mp WHERE mp.MeetingId = m.Id AND mp.ContactId = @userId AND mp.Status = @acceptedParticipantStatus)
ORDER BY ISNULL(lastMessage.SentAt, m.StartTime) DESC",
            new { userId, acceptedParticipantStatus = (int)ParticipantStatus.Accepted });

        return rows.ToList();
    }

    public async Task<MeetingThreadDto?> GetChatThreadAsync(Guid meetingId, Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<MeetingThreadDto>(@"
SELECT m.Id AS MeetingId,
       m.Description,
       m.StartTime,
       m.Location,
       m.CreatedById,
       creator.FirstName AS CreatorFirstName,
       creator.LastName AS CreatorLastName,
       (SELECT COUNT(*) FROM dbo.MeetingParticipants mp WHERE mp.MeetingId = m.Id AND mp.Status = @acceptedParticipantStatus) AS ParticipantCount
FROM dbo.Meetings m
JOIN dbo.Contacts creator ON creator.Id = m.CreatedById
WHERE m.Id = @meetingId
  AND (
      m.CreatedById = @userId
      OR EXISTS (SELECT 1 FROM dbo.MeetingParticipants mp WHERE mp.MeetingId = m.Id AND mp.ContactId = @userId AND mp.Status = @acceptedParticipantStatus)
  )", new { meetingId, userId, acceptedParticipantStatus = (int)ParticipantStatus.Accepted });
    }

    public async Task<Guid> CreateMeetingAsync(MeetingCreateDto dto, DateTime startTimeUtc)
    {
        var meetingId = Guid.NewGuid();
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var tx = connection.BeginTransaction();

        var createParams = new DynamicParameters();
        createParams.Add("@MeetingId", meetingId);
        createParams.Add("@StartTime", startTimeUtc);
        createParams.Add("@Location", dto.Location);
        createParams.Add("@Description", dto.Description);
        createParams.Add("@CategoryId", dto.CategoryId);
        createParams.Add("@CreatedById", dto.CreatedById);

        await connection.ExecuteAsync(
            "dbo.usp_Meeting_Create",
            createParams,
            tx,
            commandType: CommandType.StoredProcedure);

        var participants = new Dictionary<Guid, ParticipantStatus>
        {
            [dto.CreatedById] = ParticipantStatus.Accepted
        };

        foreach (var participant in dto.Participants ?? Array.Empty<MeetingParticipantUpdateDto>())
        {
            if (participant.ContactId == dto.CreatedById)
            {
                continue;
            }

            participants[participant.ContactId] = participant.Status;
        }

        foreach (var (contactId, status) in participants)
        {
            await connection.ExecuteAsync(
                "dbo.usp_MeetingParticipant_Upsert",
                new
                {
                    MeetingId = meetingId,
                    ContactId = contactId,
                    Status = (int)status
                },
                tx,
                commandType: CommandType.StoredProcedure);
        }

        tx.Commit();
        return meetingId;
    }

    public async Task<MeetingEditDto?> GetMeetingForEditAsync(Guid meetingId, Guid requesterId)
    {
        using var connection = connectionFactory.CreateConnection();
        var meeting = await connection.QuerySingleOrDefaultAsync<MeetingEditDto>(@"
SELECT Id, StartTime, Location, Description, CategoryId, CreatedById
FROM dbo.Meetings
WHERE Id = @meetingId AND CreatedById = @requesterId", new { meetingId, requesterId });

        if (meeting == null)
        {
            return null;
        }

        var participants = await connection.QueryAsync<MeetingParticipantDto>(@"
SELECT mp.ContactId,
       CONCAT(c.FirstName, ' ', c.LastName) AS DisplayName,
       c.Email,
       mp.Status,
       CASE WHEN mp.ContactId = m.CreatedById THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsCreator
FROM dbo.MeetingParticipants mp
JOIN dbo.Contacts c ON c.Id = mp.ContactId
JOIN dbo.Meetings m ON m.Id = mp.MeetingId
WHERE mp.MeetingId = @meetingId
ORDER BY CASE WHEN mp.ContactId = m.CreatedById THEN 0 ELSE 1 END, CONCAT(c.FirstName, ' ', c.LastName)", new { meetingId });

        meeting.Participants = participants.ToList();
        return meeting;
    }

    public async Task<MeetingDetailsDto?> GetMeetingDetailsAsync(Guid meetingId, Guid requesterId)
    {
        using var connection = connectionFactory.CreateConnection();

        var detailsLookup = new Dictionary<Guid, MeetingDetailsDto>();

        var rows = await connection.QueryAsync<MeetingDetailsDto, MeetingParticipantDto, MeetingDetailsDto>(@"
SELECT m.Id,
       m.StartTime,
       m.Location,
       m.Description,
       m.CreatedById,
       CONCAT(creator.FirstName, ' ', creator.LastName) AS CreatedByName,
       m.CategoryId,
       cat.Name AS CategoryName,
       cat.Color AS CategoryColor,
       @requesterId AS ViewerId,
       CASE WHEN m.CreatedById = @requesterId THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS ViewerIsCreator,
       CASE WHEN m.CreatedById = @requesterId OR EXISTS (SELECT 1 FROM dbo.MeetingParticipants vp WHERE vp.MeetingId = m.Id AND vp.ContactId = @requesterId) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS ViewerIsParticipant,
       (SELECT TOP 1 Status FROM dbo.MeetingParticipants vp WHERE vp.MeetingId = m.Id AND vp.ContactId = @requesterId) AS ViewerStatus,
       mp.ContactId,
       CONCAT(c.FirstName, ' ', c.LastName) AS DisplayName,
       c.Email,
       mp.Status,
       CASE WHEN mp.ContactId = m.CreatedById THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsCreator
FROM dbo.Meetings m
JOIN dbo.Contacts creator ON creator.Id = m.CreatedById
LEFT JOIN dbo.Categories cat ON cat.Id = m.CategoryId
LEFT JOIN dbo.MeetingParticipants mp ON mp.MeetingId = m.Id
LEFT JOIN dbo.Contacts c ON c.Id = mp.ContactId
WHERE m.Id = @meetingId
  AND (
      m.CreatedById = @requesterId
      OR EXISTS (SELECT 1 FROM dbo.MeetingParticipants vp WHERE vp.MeetingId = m.Id AND vp.ContactId = @requesterId)
  )
ORDER BY CASE WHEN mp.ContactId = m.CreatedById THEN 0 ELSE 1 END, CONCAT(c.FirstName, ' ', c.LastName)",
            (details, participant) =>
            {
                if (!detailsLookup.TryGetValue(details.Id, out var tracked))
                {
                    tracked = details;
                    tracked.Participants = new List<MeetingParticipantDto>();
                    detailsLookup[tracked.Id] = tracked;
                }

                if (participant != null && participant.ContactId != Guid.Empty && tracked.Participants is IList<MeetingParticipantDto> list)
                {
                    list.Add(participant);
                }

                return tracked;
            },
            new { meetingId, requesterId },
            splitOn: "ContactId");

        _ = rows.ToList();
        return detailsLookup.Values.SingleOrDefault();
    }

    public async Task<bool> UpdateMeetingAsync(MeetingUpdateDto dto, DateTime startTimeUtc)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var tx = connection.BeginTransaction();

        var owner = await connection.ExecuteScalarAsync<Guid?>("SELECT CreatedById FROM dbo.Meetings WHERE Id = @id", new { id = dto.Id }, tx);
        if (!owner.HasValue || owner.Value != dto.UpdatedById)
        {
            tx.Rollback();
            return false;
        }

        await connection.ExecuteAsync(
            "dbo.usp_Meeting_Update",
            new
            {
                MeetingId = dto.Id,
                StartTime = startTimeUtc,
                Location = dto.Location,
                Description = dto.Description,
                CategoryId = dto.CategoryId
            },
            tx,
            commandType: CommandType.StoredProcedure);

        var incoming = (dto.Participants ?? Array.Empty<MeetingParticipantUpdateDto>())
            .GroupBy(p => p.ContactId)
            .ToDictionary(g => g.Key, g => g.Last().Status);
        incoming[dto.UpdatedById] = ParticipantStatus.Accepted;

        var existing = (await connection.QueryAsync<MeetingParticipantRecord>(
                "SELECT Id, MeetingId, ContactId, Status FROM dbo.MeetingParticipants WHERE MeetingId = @meetingId",
                new { meetingId = dto.Id },
                tx))
            .ToDictionary(x => x.ContactId, x => (ParticipantStatus)x.Status);

        foreach (var existingParticipant in existing)
        {
            if (!incoming.TryGetValue(existingParticipant.Key, out var incomingStatus))
            {
                await connection.ExecuteAsync(
                    "dbo.usp_MeetingParticipant_Delete",
                    new { MeetingId = dto.Id, ContactId = existingParticipant.Key },
                    tx,
                    commandType: CommandType.StoredProcedure);

                continue;
            }

            await connection.ExecuteAsync(
                "dbo.usp_MeetingParticipant_Upsert",
                new { MeetingId = dto.Id, ContactId = existingParticipant.Key, Status = (int)incomingStatus },
                tx,
                commandType: CommandType.StoredProcedure);

            incoming.Remove(existingParticipant.Key);
        }

        foreach (var added in incoming)
        {
            await connection.ExecuteAsync(
                "dbo.usp_MeetingParticipant_Upsert",
                new { MeetingId = dto.Id, ContactId = added.Key, Status = (int)added.Value },
                tx,
                commandType: CommandType.StoredProcedure);
        }

        tx.Commit();
        return true;
    }

    public async Task<IReadOnlyCollection<ContactSuggestionViewModel>> SearchContactsAsync(Guid requesterId, string term, IEnumerable<Guid> excludeIds)
    {
        var excluded = excludeIds?.Distinct().ToArray() ?? Array.Empty<Guid>();
        if (excluded.Length == 0)
        {
            excluded = [Guid.Empty];
        }

        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<ContactSuggestionViewModel>(@"
SELECT c.Id,
       CONCAT(c.FirstName, ' ', c.LastName) AS DisplayName,
       c.Email
FROM dbo.Contacts c
WHERE c.Id <> @requesterId
  AND c.Id NOT IN @excluded
  AND (LOWER(c.FirstName) LIKE @term OR LOWER(c.LastName) LIKE @term OR LOWER(c.Email) LIKE @term)
ORDER BY c.FirstName, c.LastName", new { requesterId, excluded, term = $"%{term.Trim().ToLower()}%" });

        return rows.ToList();
    }

    public async Task<IReadOnlyCollection<ContactSuggestionViewModel>> GetContactsAsync(IEnumerable<Guid> ids)
    {
        var values = ids?.Distinct().ToArray() ?? Array.Empty<Guid>();
        if (values.Length == 0)
        {
            return Array.Empty<ContactSuggestionViewModel>();
        }

        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<ContactSuggestionViewModel>(@"
SELECT c.Id,
       CONCAT(c.FirstName, ' ', c.LastName) AS DisplayName,
       c.Email
FROM dbo.Contacts c
WHERE c.Id IN @values", new { values });

        return rows.ToList();
    }

    public async Task<(IReadOnlyCollection<MeetingSummaryDto> UpcomingMeetings, IReadOnlyCollection<MeetingSummaryDto> PastMeetings)> GetMeetingsForUserAsync(Guid userId, string? searchTerm = null)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<MeetingSummaryDto>(@"
SELECT m.Id,
       m.StartTime,
       m.Location,
       m.Description,
       m.CreatedById,
       CONCAT(creator.FirstName, ' ', creator.LastName) AS CreatedByName,
       m.CategoryId,
       cat.Name AS CategoryName,
       cat.Color AS CategoryColor,
       CASE WHEN m.CreatedById = @userId THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS ViewerIsCreator,
       CASE WHEN m.CreatedById = @userId THEN @acceptedStatus ELSE (SELECT TOP 1 mp.Status FROM dbo.MeetingParticipants mp WHERE mp.MeetingId = m.Id AND mp.ContactId = @userId) END AS ViewerStatus,
       (SELECT COUNT(*) FROM dbo.MeetingParticipants mp2 WHERE mp2.MeetingId = m.Id) AS ParticipantCount
FROM dbo.Meetings m
JOIN dbo.Contacts creator ON creator.Id = m.CreatedById
LEFT JOIN dbo.Categories cat ON cat.Id = m.CategoryId
WHERE (
    m.CreatedById = @userId
    OR EXISTS (SELECT 1 FROM dbo.MeetingParticipants mp WHERE mp.MeetingId = m.Id AND mp.ContactId = @userId)
)
AND (
    @searchTerm IS NULL
    OR LOWER(ISNULL(m.Description, '')) LIKE @pattern
    OR LOWER(ISNULL(m.Location, '')) LIKE @pattern
)
ORDER BY m.StartTime DESC", new { userId, searchTerm, pattern = $"%{(searchTerm ?? string.Empty).Trim().ToLower()}%", acceptedStatus = (int)ParticipantStatus.Accepted });

        var now = DateTime.UtcNow;
        var all = rows.ToList();
        var upcoming = all.Where(m => m.StartTime >= now).OrderBy(m => m.StartTime).ToList();
        var past = all.Where(m => m.StartTime < now).OrderByDescending(m => m.StartTime).ToList();
        return (upcoming, past);
    }

    public async Task<bool> UpdateParticipantStatusAsync(Guid meetingId, Guid participantId, ParticipantStatus status)
    {
        using var connection = connectionFactory.CreateConnection();

        var isCreator = await connection.ExecuteScalarAsync<bool>(@"
SELECT CASE WHEN EXISTS(
    SELECT 1
    FROM dbo.Meetings
    WHERE Id = @meetingId AND CreatedById = @participantId
) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END", new { meetingId, participantId });

        if (isCreator)
        {
            return false;
        }

        var affected = await connection.ExecuteAsync(
            "dbo.usp_MeetingParticipant_UpdateStatus",
            new
            {
                MeetingId = meetingId,
                ContactId = participantId,
                Status = (int)status
            },
            commandType: CommandType.StoredProcedure);

        return affected > 0;
    }

    public async Task<bool> CategoryExistsAsync(Guid categoryId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(@"
SELECT CASE WHEN EXISTS(SELECT 1 FROM dbo.Categories WHERE Id = @categoryId)
            THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END", new { categoryId });
    }

    public async Task<string?> GetContactFullNameAsync(Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<string>(@"
SELECT CONCAT(FirstName, ' ', LastName)
FROM dbo.Contacts
WHERE Id = @userId", new { userId });
    }

    public async Task<IReadOnlyCollection<Guid>> GetParticipantIdsAsync(Guid meetingId)
    {
        using var connection = connectionFactory.CreateConnection();
        var ids = await connection.QueryAsync<Guid>("SELECT ContactId FROM dbo.MeetingParticipants WHERE MeetingId = @meetingId", new { meetingId });
        return ids.Distinct().ToList();
    }

    public async Task<IReadOnlyCollection<Guid>> GetNewlyAddedParticipantIdsAsync(Guid meetingId, IEnumerable<Guid> beforeParticipantIds)
    {
        var before = beforeParticipantIds?.Distinct().ToArray() ?? Array.Empty<Guid>();
        if (before.Length == 0)
        {
            return await GetParticipantIdsAsync(meetingId);
        }

        using var connection = connectionFactory.CreateConnection();
        var ids = await connection.QueryAsync<Guid>(@"
SELECT ContactId
FROM dbo.MeetingParticipants
WHERE MeetingId = @meetingId
  AND ContactId NOT IN @before", new { meetingId, before });

        return ids.Distinct().ToList();
    }

    public async Task<DateTime?> GetMeetingStartTimeAsync(Guid meetingId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<DateTime?>("SELECT StartTime FROM dbo.Meetings WHERE Id = @meetingId", new { meetingId });
    }

    public async Task<string?> GetMeetingLocationAsync(Guid meetingId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<string?>("SELECT Location FROM dbo.Meetings WHERE Id = @meetingId", new { meetingId });
    }

}
