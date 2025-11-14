using CalendarApp.Data.Models;
using CalendarApp.Services.Meetings.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        Task<(IReadOnlyCollection<MeetingSummaryDto> UpcomingMeetings, IReadOnlyCollection<MeetingSummaryDto> PastMeetings)> GetMeetingsForUserAsync(Guid userId, string? searchTerm = null);

        Task<bool> UpdateParticipantStatusAsync(Guid meetingId, Guid participantId, ParticipantStatus status);

        Task<IReadOnlyCollection<MeetingThreadDto>> GetChatThreadsAsync(Guid userId);

        Task<MeetingThreadDto?> GetChatThreadAsync(Guid meetingId, Guid userId);
    }
}
