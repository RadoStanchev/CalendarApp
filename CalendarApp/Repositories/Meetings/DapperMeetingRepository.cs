using System.Data;
using CalendarApp.Infrastructure.Data;
using CalendarApp.Models.Meetings;
using CalendarApp.Services.Meetings.Models;
using Dapper;

namespace CalendarApp.Repositories.Meetings;

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
        return await connection.QuerySingleOrDefaultAsync<MeetingRecord>(
            "dbo.usp_Meeting_GetById",
            new { MeetingId = meetingId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<IReadOnlyCollection<MeetingThreadDto>> GetChatThreadsAsync(Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<MeetingThreadDto>(
            "dbo.usp_Meeting_GetChatThreads",
            new { UserId = userId, AcceptedParticipantStatus = (int)ParticipantStatus.Accepted },
            commandType: CommandType.StoredProcedure);

        return rows.ToList();
    }

    public async Task<MeetingThreadDto?> GetChatThreadAsync(Guid meetingId, Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<MeetingThreadDto>(
            "dbo.usp_Meeting_GetChatThread",
            new { MeetingId = meetingId, UserId = userId, AcceptedParticipantStatus = (int)ParticipantStatus.Accepted },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<Guid> CreateMeetingAsync(MeetingCreateDto dto, DateTime startTimeUtc)
    {
        var meetingId = Guid.NewGuid();
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var tx = connection.BeginTransaction();

        await connection.ExecuteAsync(
            "dbo.usp_Meeting_Create",
            new
            {
                MeetingId = meetingId,
                StartTime = startTimeUtc,
                Location = dto.Location,
                Description = dto.Description,
                CategoryId = dto.CategoryId,
                CreatedById = dto.CreatedById
            },
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
                new { MeetingId = meetingId, ContactId = contactId, Status = (int)status },
                tx,
                commandType: CommandType.StoredProcedure);
        }

        tx.Commit();
        return meetingId;
    }

    public async Task<MeetingEditDto?> GetMeetingForEditAsync(Guid meetingId, Guid requesterId)
    {
        using var connection = connectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(
            "dbo.usp_Meeting_GetForEdit",
            new { MeetingId = meetingId, RequesterId = requesterId },
            commandType: CommandType.StoredProcedure);

        var meeting = await multi.ReadSingleOrDefaultAsync<MeetingEditDto>();
        if (meeting == null)
        {
            return null;
        }

        meeting.Participants = (await multi.ReadAsync<MeetingParticipantDto>()).ToList();
        return meeting;
    }

    public async Task<MeetingDetailsDto?> GetMeetingDetailsAsync(Guid meetingId, Guid requesterId)
    {
        using var connection = connectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(
            "dbo.usp_Meeting_GetDetails",
            new { MeetingId = meetingId, RequesterId = requesterId },
            commandType: CommandType.StoredProcedure);

        var details = await multi.ReadSingleOrDefaultAsync<MeetingDetailsDto>();
        if (details == null)
        {
            return null;
        }

        details.Participants = (await multi.ReadAsync<MeetingParticipantDto>()).ToList();
        return details;
    }

    public async Task<bool> UpdateMeetingAsync(MeetingUpdateDto dto, DateTime startTimeUtc)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var tx = connection.BeginTransaction();

        var owner = await connection.ExecuteScalarAsync<Guid?>(
            "dbo.usp_Meeting_GetOwnerId",
            new { MeetingId = dto.Id },
            tx,
            commandType: CommandType.StoredProcedure);

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
                "dbo.usp_MeetingParticipant_GetByMeetingId",
                new { MeetingId = dto.Id },
                tx,
                commandType: CommandType.StoredProcedure))
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
        var excludedCsv = excluded.Length == 0 ? null : string.Join(',', excluded);

        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<ContactSuggestionViewModel>(
            "dbo.usp_Meeting_SearchContacts",
            new { RequesterId = requesterId, SearchTerm = term.Trim().ToLowerInvariant(), ExcludedIds = excludedCsv },
            commandType: CommandType.StoredProcedure);

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
        var rows = await connection.QueryAsync<ContactSuggestionViewModel>(
            "dbo.usp_Meeting_GetContacts",
            new { Ids = string.Join(',', values) },
            commandType: CommandType.StoredProcedure);

        return rows.ToList();
    }

    public async Task<(IReadOnlyCollection<MeetingSummaryDto> UpcomingMeetings, IReadOnlyCollection<MeetingSummaryDto> PastMeetings)> GetMeetingsForUserAsync(Guid userId, string? searchTerm = null)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<MeetingSummaryDto>(
            "dbo.usp_Meeting_GetMeetingsForUser",
            new { UserId = userId, SearchTerm = searchTerm, AcceptedStatus = (int)ParticipantStatus.Accepted },
            commandType: CommandType.StoredProcedure);

        var now = DateTime.UtcNow;
        var all = rows.ToList();
        var upcoming = all.Where(m => m.StartTime >= now).OrderBy(m => m.StartTime).ToList();
        var past = all.Where(m => m.StartTime < now).OrderByDescending(m => m.StartTime).ToList();
        return (upcoming, past);
    }

    public async Task<bool> UpdateParticipantStatusAsync(Guid meetingId, Guid participantId, ParticipantStatus status)
    {
        using var connection = connectionFactory.CreateConnection();
        var isCreator = await connection.ExecuteScalarAsync<bool>(
            "dbo.usp_Meeting_IsCreator",
            new { MeetingId = meetingId, ParticipantId = participantId },
            commandType: CommandType.StoredProcedure);

        if (isCreator)
        {
            return false;
        }

        var affected = await connection.ExecuteAsync(
            "dbo.usp_MeetingParticipant_UpdateStatus",
            new { MeetingId = meetingId, ContactId = participantId, Status = (int)status },
            commandType: CommandType.StoredProcedure);

        return affected > 0;
    }

    public async Task<IReadOnlyCollection<Guid>> GetParticipantIdsAsync(Guid meetingId)
    {
        using var connection = connectionFactory.CreateConnection();
        var ids = await connection.QueryAsync<Guid>(
            "dbo.usp_MeetingParticipant_GetIds",
            new { MeetingId = meetingId },
            commandType: CommandType.StoredProcedure);

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
        var ids = await connection.QueryAsync<Guid>(
            "dbo.usp_MeetingParticipant_GetNewlyAddedIds",
            new { MeetingId = meetingId, BeforeIds = string.Join(',', before) },
            commandType: CommandType.StoredProcedure);

        return ids.Distinct().ToList();
    }

    public async Task<DateTime?> GetMeetingStartTimeAsync(Guid meetingId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<DateTime?>(
            "dbo.usp_Meeting_GetStartTime",
            new { MeetingId = meetingId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<string?> GetMeetingLocationAsync(Guid meetingId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<string?>(
            "dbo.usp_Meeting_GetLocation",
            new { MeetingId = meetingId },
            commandType: CommandType.StoredProcedure);
    }
}
