using AutoMapper;
using CalendarApp.Data;
using CalendarApp.Data.Models;
using CalendarApp.Models.Meetings;
using CalendarApp.Infrastructure.Time;
using CalendarApp.Services.Meetings.Models;
using CalendarApp.Services.Notifications;
using CalendarApp.Services.Notifications.Models;
using Microsoft.EntityFrameworkCore;

namespace CalendarApp.Services.Meetings
{
    public class MeetingService : IMeetingService
    {
        private readonly ApplicationDbContext db;
        private readonly INotificationService notificationService;
        private readonly IMapper mapper;

        public MeetingService(ApplicationDbContext db, INotificationService notificationService, IMapper mapper)
        {
            this.db = db;
            this.notificationService = notificationService;
            this.mapper = mapper;
        }

        public async Task<IReadOnlyCollection<MeetingThreadDto>> GetChatThreadsAsync(Guid userId)
        {
            var meetings = await db.Meetings
                .AsNoTracking()
                .Where(m => m.CreatedById == userId
                            || m.Participants.Any(p => p.ContactId == userId && p.Status == ParticipantStatus.Accepted))
                .Select(m => new
                {
                    m.Id,
                    m.Description,
                    m.StartTime,
                    m.Location,
                    m.CreatedById,
                    CreatorFirstName = m.CreatedBy.FirstName,
                    CreatorLastName = m.CreatedBy.LastName,
                    ParticipantCount = m.Participants.Count(p => p.Status == ParticipantStatus.Accepted)
                })
                .ToListAsync();

            if (meetings.Count == 0)
            {
                return Array.Empty<MeetingThreadDto>();
            }

            var meetingIds = meetings.Select(m => m.Id).ToList();

            var latestMessages = await db.Messages
                .AsNoTracking()
                .Where(m => m.MeetingId != null && meetingIds.Contains(m.MeetingId.Value))
                .OrderByDescending(m => m.SentAt)
                .Select(m => new
                {
                    MeetingId = m.MeetingId!.Value,
                    m.Content,
                    m.SentAt
                })
                .ToListAsync();

            var latestMessageLookup = latestMessages
                .GroupBy(m => m.MeetingId)
                .ToDictionary(g => g.Key, g => g.First());

            return meetings
                .Select(m =>
                {
                    latestMessageLookup.TryGetValue(m.Id, out var lastMessage);

                    return new MeetingThreadDto
                    {
                        MeetingId = m.Id,
                        Description = m.Description,
                        StartTime = m.StartTime,
                        Location = m.Location,
                        CreatedById = m.CreatedById,
                        CreatorFirstName = m.CreatorFirstName,
                        CreatorLastName = m.CreatorLastName,
                        ParticipantCount = m.ParticipantCount,
                        LastMessageContent = lastMessage?.Content,
                        LastMessageSentAt = lastMessage?.SentAt
                    };
                })
                .ToList();
        }

        public async Task<MeetingThreadDto?> GetChatThreadAsync(Guid meetingId, Guid userId)
        {
            return await db.Meetings
                .AsNoTracking()
                .Where(m => m.Id == meetingId
                            && (m.CreatedById == userId
                                || m.Participants.Any(p => p.ContactId == userId && p.Status == ParticipantStatus.Accepted)))
                .Select(m => new MeetingThreadDto
                {
                    MeetingId = m.Id,
                    Description = m.Description,
                    StartTime = m.StartTime,
                    Location = m.Location,
                    CreatedById = m.CreatedById,
                    CreatorFirstName = m.CreatedBy.FirstName,
                    CreatorLastName = m.CreatedBy.LastName,
                    ParticipantCount = m.Participants.Count(p => p.Status == ParticipantStatus.Accepted)
                })
                .FirstOrDefaultAsync();
        }

        public async Task<Guid> CreateMeetingAsync(MeetingCreateDto dto)
        {
            var meeting = new Meeting
            {
                StartTime = BulgarianTime.ConvertLocalToUtc(dto.StartTime),
                Location = dto.Location,
                Description = dto.Description,
                CreatedById = dto.CreatedById,
            };

            meeting.CategoryId = await EnsureValidCategoryIdAsync(dto.CategoryId);

            meeting.Participants.Add(new MeetingParticipant
            {
                ContactId = dto.CreatedById,
                Status = ParticipantStatus.Accepted
            });

            if (dto.Participants != null)
            {
                foreach (var participant in dto.Participants)
                {
                    if (participant.ContactId == dto.CreatedById)
                    {
                        continue;
                    }

                    if (meeting.Participants.Any(p => p.ContactId == participant.ContactId))
                    {
                        continue;
                    }

                    meeting.Participants.Add(new MeetingParticipant
                    {
                        ContactId = participant.ContactId,
                        Status = participant.Status
                    });
                }
            }

            db.Meetings.Add(meeting);
            await db.SaveChangesAsync();

            var creatorName = $"{meeting.CreatedBy.FirstName} {meeting.CreatedBy.LastName}";
            var startTime = BulgarianTime.ConvertUtcToLocal(meeting.StartTime).ToString("g");

            var notifications = meeting.Participants
                .Where(p => p.ContactId != dto.CreatedById)
                .Select(p => new NotificationCreateDto
                {
                    UserId = p.ContactId,
                    Message = $"{creatorName} ви покани на среща на {startTime}{LocationSuffix(meeting.Location)}.",
                    Type = NotificationType.Invitation
                })
                .ToList();

            if (notifications.Count > 0)
            {
                await notificationService.CreateNotificationsAsync(notifications);
            }

            return meeting.Id;
        }

        public async Task<MeetingDetailsDto?> GetMeetingDetailsAsync(Guid meetingId, Guid requesterId)
        {
            var meeting = await db.Meetings
                .AsNoTracking()
                .Include(m => m.CreatedBy)
                .Include(m => m.Category)
                .Include(m => m.Participants)
                    .ThenInclude(p => p.Contact)
                .FirstOrDefaultAsync(m => m.Id == meetingId);

            if (meeting == null)
            {
                return null;
            }

            var viewerIsCreator = meeting.CreatedById == requesterId;
            var viewerIsParticipant = meeting.Participants.Any(p => p.ContactId == requesterId);
            var viewerParticipant = meeting.Participants.FirstOrDefault(p => p.ContactId == requesterId);
            var viewerStatus = viewerParticipant?.Status;

            if (!viewerIsCreator && !viewerIsParticipant)
            {
                return null;
            }

            var participants = meeting.Participants
                .Select(p => new MeetingParticipantDto
                {
                    ContactId = p.ContactId,
                    DisplayName = $"{p.Contact.FirstName} {p.Contact.LastName}",
                    Email = p.Contact.Email ?? string.Empty,
                    Status = p.Status,
                    IsCreator = p.ContactId == meeting.CreatedById
                })
                .OrderBy(p => p.IsCreator ? 0 : 1)
                .ThenBy(p => p.DisplayName)
                .ToList();

            return new MeetingDetailsDto
            {
                Id = meeting.Id,
                StartTime = meeting.StartTime,
                Location = meeting.Location,
                Description = meeting.Description,
                CreatedById = meeting.CreatedById,
                CreatedByName = $"{meeting.CreatedBy.FirstName} {meeting.CreatedBy.LastName}",
                CategoryId = meeting.CategoryId,
                CategoryName = meeting.Category?.Name,
                CategoryColor = meeting.Category?.Color,
                ViewerId = requesterId,
                ViewerIsCreator = viewerIsCreator,
                ViewerIsParticipant = viewerIsParticipant,
                ViewerStatus = viewerStatus,
                Participants = participants
            };
        }

        public async Task<MeetingEditDto?> GetMeetingForEditAsync(Guid meetingId, Guid requesterId)
        {
            var meeting = await db.Meetings
                .AsNoTracking()
                .Include(m => m.Participants)
                    .ThenInclude(p => p.Contact)
                .FirstOrDefaultAsync(m => m.Id == meetingId);

            if (meeting == null || meeting.CreatedById != requesterId)
            {
                return null;
            }

            var startTimeLocal = BulgarianTime.ConvertUtcToLocal(meeting.StartTime);

            var participants = meeting.Participants
                .Select(p => new MeetingParticipantDto
                {
                    ContactId = p.ContactId,
                    DisplayName = $"{p.Contact.FirstName} {p.Contact.LastName}",
                    Email = p.Contact.Email ?? string.Empty,
                    Status = p.Status,
                    IsCreator = p.ContactId == meeting.CreatedById
                })
                .OrderBy(p => p.IsCreator ? 0 : 1)
                .ThenBy(p => p.DisplayName)
                .ToList();

            return new MeetingEditDto
            {
                Id = meeting.Id,
                StartTime = startTimeLocal,
                Location = meeting.Location,
                Description = meeting.Description,
                CategoryId = meeting.CategoryId,
                CreatedById = meeting.CreatedById,
                Participants = participants
            };
        }

        public async Task<IReadOnlyCollection<ContactSuggestionViewModel>> SearchContactsAsync(Guid requesterId, string term, IEnumerable<Guid> excludeIds)
        {
            term = term?.Trim() ?? string.Empty;
            var exclude = new HashSet<Guid>(excludeIds ?? Enumerable.Empty<Guid>()) { requesterId };

            var query = db.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(term))
            {
                var pattern = $"%{term}%";
                query = query.Where(u => EF.Functions.Like(u.FirstName ?? string.Empty, pattern)
                    || EF.Functions.Like(u.LastName ?? string.Empty, pattern)
                    || EF.Functions.Like(u.Email ?? string.Empty, pattern));
            }

            var suggestions = await query
                .Where(u => !exclude.Contains(u.Id))
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Take(10)
                .Select(u => new ContactSuggestionViewModel
                {
                    Id = u.Id,
                    DisplayName = $"{u.FirstName} {u.LastName}",
                    Email = u.Email ?? string.Empty
                })
                .ToListAsync();

            return suggestions;
        }

        public async Task<IReadOnlyCollection<ContactSuggestionViewModel>> GetContactsAsync(IEnumerable<Guid> ids)
        {
            var idList = ids?.Distinct().ToList() ?? [];
            if (idList.Count == 0)
            {
                return Array.Empty<ContactSuggestionViewModel>();
            }

            var contacts = await db.Users
                .AsNoTracking()
                .Where(u => idList.Contains(u.Id))
                .Select(u => new ContactSuggestionViewModel
                {
                    Id = u.Id,
                    DisplayName = $"{u.FirstName} {u.LastName}",
                    Email = u.Email ?? string.Empty
                })
                .ToListAsync();

            return contacts;
        }

        public async Task<(IReadOnlyCollection<MeetingSummaryDto> UpcomingMeetings, IReadOnlyCollection<MeetingSummaryDto> PastMeetings)> GetMeetingsForUserAsync(Guid userId, string? searchTerm = null)
        {
            var utcNow = DateTime.UtcNow;

            var query = db.Meetings
                .AsNoTracking()
                .Where(m => m.CreatedById == userId || m.Participants.Any(p => p.ContactId == userId));

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var trimmed = searchTerm.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    var pattern = $"%{trimmed}%";
                    query = query.Where(m =>
                        EF.Functions.Like(m.Description ?? string.Empty, pattern) ||
                        EF.Functions.Like(m.Location ?? string.Empty, pattern));
                }
            }

            var baseQuery = query
                .Include(m => m.CreatedBy)
                .Include(m => m.Category)
                .Include(m => m.Participants)
                .AsSplitQuery();

            var upcomingMeetings = await baseQuery
                .Where(m => m.StartTime >= utcNow)
                .OrderBy(m => m.StartTime)
                .ToListAsync();

            var pastMeetings = await baseQuery
                .Where(m => m.StartTime < utcNow)
                .OrderByDescending(m => m.StartTime)
                .ToListAsync();

            var upcoming = mapper.Map<List<MeetingSummaryDto>>(upcomingMeetings, opts =>
            {
                opts.Items["ViewerId"] = userId;
            });

            var past = mapper.Map<List<MeetingSummaryDto>>(pastMeetings, opts =>
            {
                opts.Items["ViewerId"] = userId;
            });

            return (upcoming, past);
        }

        public async Task<bool> UpdateMeetingAsync(MeetingUpdateDto dto)
        {
            var meeting = await db.Meetings
             .Include(m => m.CreatedBy)
             .Include(m => m.Participants)
             .FirstOrDefaultAsync(m => m.Id == dto.Id);

            if (meeting == null || meeting.CreatedById != dto.UpdatedById)
                return false;

            // Update basic fields
            var newStartUtc = BulgarianTime.ConvertLocalToUtc(dto.StartTime);

            if (meeting.StartTime != newStartUtc && newStartUtc > DateTime.UtcNow)
                meeting.ReminderSent = false;

            meeting.StartTime = newStartUtc;
            meeting.Location = dto.Location;
            meeting.Description = dto.Description;
            meeting.CategoryId = await EnsureValidCategoryIdAsync(dto.CategoryId);

            // Build participants map (deduplicated)
            var incoming = dto.Participants?
                .GroupBy(p => p.ContactId)
                .ToDictionary(g => g.Key, g => g.Last().Status)
                ?? new Dictionary<Guid, ParticipantStatus>();

            // Creator must always be Accepted
            incoming[dto.UpdatedById] = ParticipantStatus.Accepted;

            var existing = meeting.Participants.ToDictionary(p => p.ContactId);
            var newlyAdded = new List<Guid>();

            // Update current participants
            foreach (var (contactId, participant) in existing)
            {
                if (!incoming.TryGetValue(contactId, out var status))
                {
                    meeting.Participants.Remove(participant);
                    continue;
                }

                participant.Status = status;
                incoming.Remove(contactId);
            }

            // Add new participants
            foreach (var (contactId, status) in incoming)
            {
                if (contactId == meeting.CreatedById) continue;

                db.MeetingParticipants.Add(new MeetingParticipant
                {
                    MeetingId = meeting.Id,
                    ContactId = contactId,
                    Status = status
                });

                if (!existing.ContainsKey(contactId))
                    newlyAdded.Add(contactId);
            }

            await db.SaveChangesAsync();

            // --- Notifications ---
            var updaterName = $"{meeting.CreatedBy.FirstName} {meeting.CreatedBy.LastName}";
            var startStr = BulgarianTime.ConvertUtcToLocal(meeting.StartTime).ToString("g");
            var location = string.IsNullOrWhiteSpace(meeting.Location)
                ? ""
                : $" на {meeting.Location.Trim()}";

            // Invitations (only new participants)
            if (newlyAdded.Count > 0)
            {
                await notificationService.CreateNotificationsAsync(
                    newlyAdded
                        .Where(id => id != dto.UpdatedById)
                        .Select(id => new NotificationCreateDto
                        {
                            UserId = id,
                            Message = $"{updaterName} ви добави към среща на {startStr}{location}.",
                            Type = NotificationType.Invitation
                        })
                );
            }

            // Updates for existing participants (exclude creator, updater, newly added)
            await notificationService.CreateNotificationsAsync(
                meeting.Participants
                    .Select(p => p.ContactId)
                    .Where(id => id != dto.UpdatedById &&
                                 id != meeting.CreatedById &&
                                 !newlyAdded.Contains(id))
                    .Distinct()
                    .Select(id => new NotificationCreateDto
                    {
                        UserId = id,
                        Message = $"{updaterName} актуализира срещата на {startStr}{location}.",
                        Type = NotificationType.Info
                    })
            );

            return true;
        }

        public async Task<bool> UpdateParticipantStatusAsync(Guid meetingId, Guid participantId, ParticipantStatus status)
        {
            var participant = await db.MeetingParticipants
                .Include(p => p.Meeting)
                .FirstOrDefaultAsync(p => p.MeetingId == meetingId && p.ContactId == participantId);

            if (participant == null || participant.Meeting.CreatedById == participantId)
            {
                return false;
            }

            if (participant.Status == status)
            {
                return true;
            }

            participant.Status = status;
            await db.SaveChangesAsync();
            return true;
        }

        private async Task<Guid> EnsureValidCategoryIdAsync(Guid categoryId)
        {
            if (categoryId == Guid.Empty)
            {
                throw new ArgumentException("Изисква се категория.", nameof(categoryId));
            }

            var exists = await db.Categories
                .AsNoTracking()
                .AnyAsync(c => c.Id == categoryId);

            if (!exists)
            {
                throw new ArgumentException("Избраната категория не съществува.", nameof(categoryId));
            }

            return categoryId;
        }

        private static string LocationSuffix(string? location)
            => string.IsNullOrWhiteSpace(location) ? "" : $" на {location.Trim()}";
    }
}
