using AutoMapper;
using CalendarApp.Data;
using CalendarApp.Data.Models;
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
                StartTime = dto.StartTime,
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

            await db.Entry(meeting)
                .Reference(m => m.CreatedBy)
                .LoadAsync();

            var creatorName = $"{meeting.CreatedBy.FirstName} {meeting.CreatedBy.LastName}";
            var startTime = meeting.StartTime.ToLocalTime().ToString("g");

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
                StartTime = meeting.StartTime,
                Location = meeting.Location,
                Description = meeting.Description,
                CategoryId = meeting.CategoryId,
                CreatedById = meeting.CreatedById,
                Participants = participants
            };
        }

        public async Task<IReadOnlyCollection<ContactSuggestionDto>> SearchContactsAsync(Guid requesterId, string term, IEnumerable<Guid> excludeIds)
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
                .Select(u => new ContactSuggestionDto
                {
                    Id = u.Id,
                    DisplayName = $"{u.FirstName} {u.LastName}",
                    Email = u.Email ?? string.Empty
                })
                .ToListAsync();

            return suggestions;
        }

        public async Task<IReadOnlyCollection<ContactSummaryDto>> GetContactsAsync(IEnumerable<Guid> ids)
        {
            var idList = ids?.Distinct().ToList() ?? [];
            if (idList.Count == 0)
            {
                return Array.Empty<ContactSummaryDto>();
            }

            var contacts = await db.Users
                .AsNoTracking()
                .Where(u => idList.Contains(u.Id))
                .Select(u => new ContactSummaryDto
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
                    .ThenInclude(p => p.Contact)
                .FirstOrDefaultAsync(m => m.Id == dto.Id);

            if (meeting == null || meeting.CreatedById != dto.UpdatedById)
            {
                return false;
            }

            var originalStartTime = meeting.StartTime;
            var existingParticipantIds = meeting.Participants
                .Select(p => p.ContactId)
                .ToHashSet();

            meeting.StartTime = dto.StartTime;
            meeting.Location = dto.Location;
            meeting.Description = dto.Description;
            meeting.CategoryId = await EnsureValidCategoryIdAsync(dto.CategoryId);

            if (originalStartTime != dto.StartTime && dto.StartTime > DateTime.Now)
            {
                meeting.ReminderSent = false;
            }

            var incomingParticipants = (dto.Participants ?? Array.Empty<MeetingParticipantUpdateDto>())
                .GroupBy(p => p.ContactId)
                .Select(g => new MeetingParticipantUpdateDto
                {
                    ContactId = g.Key,
                    Status = g.Last().Status
                })
                .ToDictionary(p => p.ContactId, p => p.Status);

            incomingParticipants[dto.UpdatedById] = ParticipantStatus.Accepted;

            var existingParticipants = meeting.Participants.ToList();
            var newlyAddedParticipantIds = new List<Guid>();
            foreach (var participant in existingParticipants)
            {
                if (participant.ContactId == meeting.CreatedById)
                {
                    participant.Status = ParticipantStatus.Accepted;
                    continue;
                }

                if (!incomingParticipants.ContainsKey(participant.ContactId))
                {
                    meeting.Participants.Remove(participant);
                    continue;
                }

                participant.Status = incomingParticipants[participant.ContactId];
                incomingParticipants.Remove(participant.ContactId);
            }

            foreach (var (contactId, status) in incomingParticipants)
            {
                if (contactId == meeting.CreatedById)
                {
                    continue;
                }

                meeting.Participants.Add(new MeetingParticipant
                {
                    ContactId = contactId,
                    Status = status
                });

                if (!existingParticipantIds.Contains(contactId))
                {
                    newlyAddedParticipantIds.Add(contactId);
                }
            }

            await db.SaveChangesAsync();

            var updaterName = $"{meeting.CreatedBy.FirstName} {meeting.CreatedBy.LastName}";
            var startTime = meeting.StartTime.ToLocalTime().ToString("g");

            var invitationNotifications = newlyAddedParticipantIds
                .Where(id => id != dto.UpdatedById)
                .Select(id => new NotificationCreateDto
                {
                    UserId = id,
                    Message = $"{updaterName} ви добави към среща на {startTime}{LocationSuffix(meeting.Location)}.",
                    Type = NotificationType.Invitation
                })
                .ToList();

            if (invitationNotifications.Count > 0)
            {
                await notificationService.CreateNotificationsAsync(invitationNotifications);
            }

            var participantsToNotify = meeting.Participants
                .Select(p => p.ContactId)
                .Where(id => id != dto.UpdatedById && id != meeting.CreatedById && !newlyAddedParticipantIds.Contains(id))
                .Distinct()
                .Select(id => new NotificationCreateDto
                {
                    UserId = id,
                    Message = $"{updaterName} актуализира срещата на {startTime}{LocationSuffix(meeting.Location)}.",
                    Type = NotificationType.Info
                })
                .ToList();

            if (participantsToNotify.Count > 0)
            {
                await notificationService.CreateNotificationsAsync(participantsToNotify);
            }

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
            => string.IsNullOrWhiteSpace(location)? "" : $" на {location.Trim()}";
    }
}
