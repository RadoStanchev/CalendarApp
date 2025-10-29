using CalendarApp.Data.Models;
using CalendarApp.Services.Meetings.Models;

namespace CalendarApp.Services.Meetings
{
    public interface IMeetingService
    {
        Task<Guid> CreateMeetingAsync(MeetingCreateDto dto);

        Task<MeetingEditDto?> GetMeetingForEditAsync(Guid meetingId, Guid requesterId);

        Task<MeetingDetailsDto?> GetMeetingDetailsAsync(Guid meetingId, Guid requesterId);

        Task<bool> UpdateMeetingAsync(MeetingUpdateDto dto);

        Task<IReadOnlyCollection<ContactSuggestionDto>> SearchContactsAsync(Guid requesterId, string term, IEnumerable<Guid> excludeIds);

        Task<IReadOnlyCollection<ContactSummaryDto>> GetContactsAsync(IEnumerable<Guid> ids);

        Task<IReadOnlyCollection<MeetingSummaryDto>> GetMeetingsForUserAsync(Guid userId);

        Task<bool> UpdateParticipantStatusAsync(Guid meetingId, Guid participantId, ParticipantStatus status);
    }
}
