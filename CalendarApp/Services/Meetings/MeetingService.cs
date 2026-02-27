using CalendarApp.Data.Models;
using CalendarApp.Infrastructure.Time;
using CalendarApp.Models.Meetings;
using CalendarApp.Services.Meetings.Models;
using CalendarApp.Services.Meetings.Repositories;
using CalendarApp.Services.Notifications;
using CalendarApp.Services.Notifications.Models;

namespace CalendarApp.Services.Meetings
{
    public class MeetingService : IMeetingService
    {
        private readonly IMeetingRepository meetingRepository;
        private readonly INotificationService notificationService;

        public MeetingService(IMeetingRepository meetingRepository, INotificationService notificationService)
        {
            this.meetingRepository = meetingRepository;
            this.notificationService = notificationService;
        }

        public Task<IReadOnlyCollection<MeetingThreadDto>> GetChatThreadsAsync(Guid userId)
            => meetingRepository.GetChatThreadsAsync(userId);

        public Task<MeetingThreadDto?> GetChatThreadAsync(Guid meetingId, Guid userId)
            => meetingRepository.GetChatThreadAsync(meetingId, userId);

        public async Task<Guid> CreateMeetingAsync(MeetingCreateDto dto)
        {
            await EnsureValidCategoryIdAsync(dto.CategoryId);
            var startUtc = BulgarianTime.ConvertLocalToUtc(dto.StartTime);
            var meetingId = await meetingRepository.CreateMeetingAsync(dto, startUtc);

            var creatorName = await meetingRepository.GetContactFullNameAsync(dto.CreatedById);
            if (!string.IsNullOrWhiteSpace(creatorName))
            {
                var startLocal = BulgarianTime.ConvertUtcToLocal(startUtc).ToString("g");
                var locationSuffix = LocationSuffix(dto.Location);
                var invitees = (await meetingRepository.GetParticipantIdsAsync(meetingId))
                    .Where(id => id != dto.CreatedById)
                    .Distinct()
                    .ToList();

                if (invitees.Count > 0)
                {
                    await notificationService.CreateNotificationsAsync(invitees.Select(id => new NotificationCreateDto
                    {
                        UserId = id,
                        Message = $"{creatorName} ви покани на среща на {startLocal}{locationSuffix}.",
                        Type = NotificationType.Invitation
                    }));
                }
            }

            return meetingId;
        }

        public async Task<MeetingEditDto?> GetMeetingForEditAsync(Guid meetingId, Guid requesterId)
        {
            var dto = await meetingRepository.GetMeetingForEditAsync(meetingId, requesterId);
            if (dto == null)
            {
                return null;
            }

            dto.StartTime = BulgarianTime.ConvertUtcToLocal(dto.StartTime);
            return dto;
        }

        public async Task<MeetingDetailsDto?> GetMeetingDetailsAsync(Guid meetingId, Guid requesterId)
        {
            var dto = await meetingRepository.GetMeetingDetailsAsync(meetingId, requesterId);
            if (dto == null)
            {
                return null;
            }

            dto.StartTime = BulgarianTime.ConvertUtcToLocal(dto.StartTime);
            return dto;
        }

        public async Task<bool> UpdateMeetingAsync(MeetingUpdateDto dto)
        {
            await EnsureValidCategoryIdAsync(dto.CategoryId);

            var beforeParticipants = await meetingRepository.GetParticipantIdsAsync(dto.Id);
            var startUtc = BulgarianTime.ConvertLocalToUtc(dto.StartTime);
            var updated = await meetingRepository.UpdateMeetingAsync(dto, startUtc);
            if (!updated)
            {
                return false;
            }

            var updaterName = await meetingRepository.GetContactFullNameAsync(dto.UpdatedById);
            if (string.IsNullOrWhiteSpace(updaterName))
            {
                return true;
            }

            var startLocal = BulgarianTime.ConvertUtcToLocal(startUtc).ToString("g");
            var locationSuffix = LocationSuffix(dto.Location);

            var currentParticipants = await meetingRepository.GetParticipantIdsAsync(dto.Id);
            var newlyAdded = (await meetingRepository.GetNewlyAddedParticipantIdsAsync(dto.Id, beforeParticipants)).Where(id => id != dto.UpdatedById).ToList();

            if (newlyAdded.Count > 0)
            {
                await notificationService.CreateNotificationsAsync(newlyAdded.Select(id => new NotificationCreateDto
                {
                    UserId = id,
                    Message = $"{updaterName} ви добави към среща на {startLocal}{locationSuffix}.",
                    Type = NotificationType.Invitation
                }));
            }

            var updateRecipients = currentParticipants
                .Where(id => id != dto.UpdatedById && !newlyAdded.Contains(id))
                .Distinct()
                .ToList();

            if (updateRecipients.Count > 0)
            {
                await notificationService.CreateNotificationsAsync(updateRecipients.Select(id => new NotificationCreateDto
                {
                    UserId = id,
                    Message = $"{updaterName} актуализира срещата на {startLocal}{locationSuffix}.",
                    Type = NotificationType.Info
                }));
            }

            return true;
        }

        public Task<IReadOnlyCollection<ContactSuggestionViewModel>> SearchContactsAsync(Guid requesterId, string term, IEnumerable<Guid> excludeIds)
            => meetingRepository.SearchContactsAsync(requesterId, term, excludeIds);

        public Task<IReadOnlyCollection<ContactSuggestionViewModel>> GetContactsAsync(IEnumerable<Guid> ids)
            => meetingRepository.GetContactsAsync(ids);

        public async Task<(IReadOnlyCollection<MeetingSummaryDto> UpcomingMeetings, IReadOnlyCollection<MeetingSummaryDto> PastMeetings)> GetMeetingsForUserAsync(Guid userId, string? searchTerm = null)
        {
            var result = await meetingRepository.GetMeetingsForUserAsync(userId, searchTerm);

            foreach (var meeting in result.UpcomingMeetings)
            {
                meeting.StartTime = BulgarianTime.ConvertUtcToLocal(meeting.StartTime);
            }

            foreach (var meeting in result.PastMeetings)
            {
                meeting.StartTime = BulgarianTime.ConvertUtcToLocal(meeting.StartTime);
            }

            return result;
        }

        public Task<bool> UpdateParticipantStatusAsync(Guid meetingId, Guid participantId, ParticipantStatus status)
            => meetingRepository.UpdateParticipantStatusAsync(meetingId, participantId, status);

        private async Task EnsureValidCategoryIdAsync(Guid categoryId)
        {
            if (categoryId == Guid.Empty)
            {
                throw new ArgumentException("Изисква се категория.", nameof(categoryId));
            }

            var exists = await meetingRepository.CategoryExistsAsync(categoryId);
            if (!exists)
            {
                throw new ArgumentException("Избраната категория не съществува.", nameof(categoryId));
            }
        }

        private static string LocationSuffix(string? location)
            => string.IsNullOrWhiteSpace(location) ? string.Empty : $" на {location.Trim()}";
    }
}
