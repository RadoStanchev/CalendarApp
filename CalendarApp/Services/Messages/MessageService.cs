using CalendarApp.Data;
using CalendarApp.Data.Models;
using CalendarApp.Services.Messages.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace CalendarApp.Services.Messages
{
    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<Contact> userManager;

        public MessageService(ApplicationDbContext db, UserManager<Contact> userManager)
        {
            this.db = db;
            this.userManager = userManager;
        }

        public string BuildFriendshipGroupName(Guid friendshipId) => $"friendship:{friendshipId}";

        public string BuildMeetingGroupName(Guid meetingId) => $"meeting:{meetingId}";

        public async Task EnsureFriendshipAccessAsync(Guid userId, Guid friendshipId)
        {
            var hasAccess = await db.Friendships
                .AsNoTracking()
                .Where(f => f.Id == friendshipId
                            && f.Status == FriendshipStatus.Accepted
                            && (f.RequesterId == userId || f.ReceiverId == userId))
                .AnyAsync();

            if (!hasAccess)
            {
                throw new InvalidOperationException("Приятелството не е намерено или достъпът е отказан.");
            }
        }

        public async Task EnsureMeetingAccessAsync(Guid userId, Guid meetingId)
        {
            var hasAccess = await db.Meetings
                .AsNoTracking()
                .Where(m => m.Id == meetingId
                            && (m.CreatedById == userId
                                || m.Participants.Any(p => p.ContactId == userId && p.Status == ParticipantStatus.Accepted)))
                .AnyAsync();

            if (!hasAccess)
            {
                throw new InvalidOperationException("Срещата не е намерена или достъпът е отказан.");
            }
        }

        public async Task<ChatMessageDto> SaveMessageAsync(Guid userId, Guid friendshipId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Съдържанието на съобщението не може да бъде празно.", nameof(content));
            }

            await EnsureFriendshipAccessAsync(userId, friendshipId);

            var sender = await GetSenderAsync(userId);

            var entity = new Message
            {
                FriendshipId = friendshipId,
                SenderId = userId,
                Content = content,
                SentAt = DateTime.UtcNow
            };

            db.Messages.Add(entity);
            await db.SaveChangesAsync();

            return new ChatMessageDto
            {
                FriendshipId = friendshipId,
                MessageId = entity.Id,
                SenderId = sender.Id,
                SenderName = sender.Name,
                Content = entity.Content,
                SentAt = entity.SentAt,
                Metadata = new Dictionary<string, string?>()
            };
        }

        public async Task<ChatMessageDto> SaveMeetingMessageAsync(Guid userId, Guid meetingId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Съдържанието на съобщението не може да бъде празно.", nameof(content));
            }

            await EnsureMeetingAccessAsync(userId, meetingId);

            var sender = await GetSenderAsync(userId);

            var meeting = await db.Meetings
                .AsNoTracking()
                .Where(m => m.Id == meetingId)
                .Select(m => new { m.Id, m.StartTime, m.Location })
                .FirstOrDefaultAsync();

            if (meeting == null)
            {
                throw new InvalidOperationException("Срещата не беше намерена.");
            }

            var entity = new Message
            {
                MeetingId = meetingId,
                SenderId = userId,
                Content = content,
                SentAt = DateTime.UtcNow
            };

            db.Messages.Add(entity);
            await db.SaveChangesAsync();

            var metadata = new Dictionary<string, string?>
            {
                ["meetingStartUtc"] = meeting.StartTime.ToUniversalTime().ToString("O"),
                ["meetingLocation"] = meeting.Location
            };

            return new ChatMessageDto
            {
                MeetingId = meetingId,
                MessageId = entity.Id,
                SenderId = sender.Id,
                SenderName = sender.Name,
                Content = entity.Content,
                SentAt = entity.SentAt,
                Metadata = metadata
            };
        }

        public async Task<IReadOnlyCollection<ChatMessageDto>> GetRecentFriendshipMessagesAsync(Guid userId, Guid friendshipId, int take)
        {
            await EnsureFriendshipAccessAsync(userId, friendshipId);

            var messages = await db.Messages
                .AsNoTracking()
                .Where(m => m.FriendshipId == friendshipId)
                .OrderByDescending(m => m.SentAt)
                .Take(take)
                .Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    m.Content,
                    m.SentAt,
                    m.FriendshipId,
                    m.MeetingId,
                    SenderFirstName = m.Sender.FirstName,
                    SenderLastName = m.Sender.LastName
                })
                .ToListAsync();

            return messages
                .Select(m => new ChatMessageDto
                {
                    FriendshipId = m.FriendshipId,
                    MeetingId = m.MeetingId,
                    MessageId = m.Id,
                    SenderId = m.SenderId,
                    SenderName = BuildDisplayName(m.SenderFirstName, m.SenderLastName),
                    Content = m.Content,
                    SentAt = m.SentAt,
                    Metadata = new Dictionary<string, string?>()
                })
                .OrderBy(m => m.SentAt)
                .ToList();
        }

        public async Task<IReadOnlyCollection<ChatMessageDto>> GetRecentMeetingMessagesAsync(Guid userId, Guid meetingId, int take)
        {
            await EnsureMeetingAccessAsync(userId, meetingId);

            var messages = await db.Messages
                .AsNoTracking()
                .Where(m => m.MeetingId == meetingId)
                .OrderByDescending(m => m.SentAt)
                .Take(take)
                .Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    m.Content,
                    m.SentAt,
                    m.FriendshipId,
                    m.MeetingId,
                    SenderFirstName = m.Sender.FirstName,
                    SenderLastName = m.Sender.LastName
                })
                .ToListAsync();

            return messages
                .Select(m => new ChatMessageDto
                {
                    FriendshipId = m.FriendshipId,
                    MeetingId = m.MeetingId,
                    MessageId = m.Id,
                    SenderId = m.SenderId,
                    SenderName = BuildDisplayName(m.SenderFirstName, m.SenderLastName),
                    Content = m.Content,
                    SentAt = m.SentAt,
                    Metadata = new Dictionary<string, string?>()
                })
                .OrderBy(m => m.SentAt)
                .ToList();
        }

        private async Task<(Guid Id, string Name)> GetSenderAsync(Guid userId)
        {
            var sender = await userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.Id, u.FirstName, u.LastName })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (sender == null)
            {
                throw new InvalidOperationException("Подателят не е намерен.");
            }

            var senderName = BuildDisplayName(sender.FirstName, sender.LastName);

            return (sender.Id, senderName);
        }

        private static string BuildDisplayName(string? firstName, string? lastName)
        {
            return string.Join(" ", new[] { firstName, lastName }
                .Select(part => part?.Trim())
                .Where(part => !string.IsNullOrEmpty(part)));
        }
    }
}
