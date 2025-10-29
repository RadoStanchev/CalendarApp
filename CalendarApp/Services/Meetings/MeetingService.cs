using CalendarApp.Data;
using CalendarApp.Data.Models;
using CalendarApp.Services.Meetings.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalendarApp.Services.Meetings
{
    public class MeetingService : IMeetingService
    {
        private readonly ApplicationDbContext db;

        public MeetingService(ApplicationDbContext db)
        {
            this.db = db;
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
            return meeting.Id;
        }

        public async Task<MeetingDetailsDto?> GetMeetingDetailsAsync(Guid meetingId, Guid requesterId)
        {
            var meeting = await db.Meetings
                .AsNoTracking()
                .Include(m => m.CreatedBy)
                .Include(m => m.Participants)
                    .ThenInclude(p => p.Contact)
                .FirstOrDefaultAsync(m => m.Id == meetingId);

            if (meeting == null)
            {
                return null;
            }

            var viewerIsCreator = meeting.CreatedById == requesterId;
            var viewerIsParticipant = meeting.Participants.Any(p => p.ContactId == requesterId);

            if (!viewerIsCreator && !viewerIsParticipant)
            {
                return null;
            }

            var participants = meeting.Participants
                .Select(p => new MeetingParticipantDto
                {
                    ContactId = p.ContactId,
                    DisplayName = FormatName(p.Contact?.FirstName, p.Contact?.LastName),
                    Email = p.Contact?.Email ?? string.Empty,
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
                CreatedByName = FormatName(meeting.CreatedBy?.FirstName, meeting.CreatedBy?.LastName),
                ViewerId = requesterId,
                ViewerIsCreator = viewerIsCreator,
                ViewerIsParticipant = viewerIsParticipant,
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
                    DisplayName = FormatName(p.Contact?.FirstName, p.Contact?.LastName),
                    Email = p.Contact?.Email ?? string.Empty,
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
                    DisplayName = FormatName(u.FirstName, u.LastName),
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
                    DisplayName = FormatName(u.FirstName, u.LastName),
                    Email = u.Email ?? string.Empty
                })
                .ToListAsync();

            return contacts;
        }

        public async Task<bool> UpdateMeetingAsync(MeetingUpdateDto dto)
        {
            var meeting = await db.Meetings
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.Id == dto.Id);

            if (meeting == null || meeting.CreatedById != dto.UpdatedById)
            {
                return false;
            }

            meeting.StartTime = dto.StartTime;
            meeting.Location = dto.Location;
            meeting.Description = dto.Description;

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
            }

            await db.SaveChangesAsync();
            return true;
        }

        private static string FormatName(string? firstName, string? lastName)
        {
            var parts = new[] { firstName, lastName }
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();
            return parts.Length > 0 ? string.Join(" ", parts) : "Unknown";
        }
    }
}
