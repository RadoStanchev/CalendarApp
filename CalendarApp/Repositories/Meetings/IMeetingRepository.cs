using CalendarApp.Models.Meetings;
using CalendarApp.Services.Meetings.Models;

namespace CalendarApp.Repositories.Meetings;

public interface IMeetingRepository
{
    Task<IReadOnlyCollection<MeetingThreadDto>> GetChatThreadsAsync(Guid userId);
    Task<MeetingThreadDto?> GetChatThreadAsync(Guid meetingId, Guid userId);
    Task<MeetingRecord?> GetByIdAsync(Guid meetingId);
    Task<Guid> CreateMeetingAsync(MeetingCreateDto dto, DateTime startTimeUtc);
    Task<MeetingEditDto?> GetMeetingForEditAsync(Guid meetingId, Guid requesterId);
    Task<MeetingDetailsDto?> GetMeetingDetailsAsync(Guid meetingId, Guid requesterId);
    Task<bool> UpdateMeetingAsync(MeetingUpdateDto dto, DateTime startTimeUtc);
    Task<IReadOnlyCollection<ContactSuggestionViewModel>> SearchContactsAsync(Guid requesterId, string term, IEnumerable<Guid> excludeIds);
    Task<IReadOnlyCollection<ContactSuggestionViewModel>> GetContactsAsync(IEnumerable<Guid> ids);
    Task<(IReadOnlyCollection<MeetingSummaryDto> UpcomingMeetings, IReadOnlyCollection<MeetingSummaryDto> PastMeetings)> GetMeetingsForUserAsync(Guid userId, string? searchTerm = null);
    Task<bool> UpdateParticipantStatusAsync(Guid meetingId, Guid participantId, int status);
    Task<IReadOnlyCollection<Guid>> GetParticipantIdsAsync(Guid meetingId);
    Task<IReadOnlyCollection<Guid>> GetNewlyAddedParticipantIdsAsync(Guid meetingId, IEnumerable<Guid> beforeParticipantIds);
    Task<DateTime?> GetMeetingStartTimeAsync(Guid meetingId);
    Task<string?> GetMeetingLocationAsync(Guid meetingId);
}
