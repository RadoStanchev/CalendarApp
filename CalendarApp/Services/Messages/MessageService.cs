using CalendarApp.Data;
using CalendarApp.Data.Models;
using CalendarApp.Services.Messages.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading;
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

        public async Task EnsureFriendshipAccessAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken = default)
        {
            var hasAccess = await db.Friendships
                .AsNoTracking()
                .Where(f => f.Id == friendshipId
                            && f.Status == FriendshipStatus.Accepted
                            && (f.RequesterId == userId || f.ReceiverId == userId))
                .AnyAsync(cancellationToken);

            if (!hasAccess)
            {
                throw new InvalidOperationException("Friendship not found or access denied.");
            }
        }

        public async Task EnsureMeetingAccessAsync(Guid userId, Guid meetingId, CancellationToken cancellationToken = default)
        {
            var hasAccess = await db.Meetings
                .AsNoTracking()
                .Where(m => m.Id == meetingId
                            && (m.CreatedById == userId
                                || m.Participants.Any(p => p.ContactId == userId && p.Status == ParticipantStatus.Accepted)))
                .AnyAsync(cancellationToken);

            if (!hasAccess)
            {
                throw new InvalidOperationException("Meeting not found or access denied.");
            }
        }

        public async Task<ChatMessageDto> SaveMessageAsync(Guid userId, Guid friendshipId, string content, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Message content must not be empty.", nameof(content));
            }

            await EnsureFriendshipAccessAsync(userId, friendshipId, cancellationToken);

            var sender = await GetSenderAsync(userId, cancellationToken);

            var entity = new Message
            {
                FriendshipId = friendshipId,
                SenderId = userId,
                Content = content,
                SentAt = DateTime.UtcNow
            };

            db.Messages.Add(entity);
            await db.SaveChangesAsync(cancellationToken);

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

        public async Task<ChatMessageDto> SaveMeetingMessageAsync(Guid userId, Guid meetingId, string content, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Message content must not be empty.", nameof(content));
            }

            await EnsureMeetingAccessAsync(userId, meetingId, cancellationToken);

            var sender = await GetSenderAsync(userId, cancellationToken);

            var meeting = await db.Meetings
                .AsNoTracking()
                .Where(m => m.Id == meetingId)
                .Select(m => new { m.Id, m.StartTime, m.Location })
                .FirstOrDefaultAsync(cancellationToken);

            if (meeting == null)
            {
                throw new InvalidOperationException("Meeting not found.");
            }

            var entity = new Message
            {
                MeetingId = meetingId,
                SenderId = userId,
                Content = content,
                SentAt = DateTime.UtcNow
            };

            db.Messages.Add(entity);
            await db.SaveChangesAsync(cancellationToken);

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

        private async Task<(Guid Id, string Name)> GetSenderAsync(Guid userId, CancellationToken cancellationToken)
        {
            var sender = await userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.Id, u.FirstName, u.LastName })
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (sender == null)
            {
                throw new InvalidOperationException("Sender not found.");
            }

            var senderName = string.Join(" ", new[] { sender.FirstName, sender.LastName }
                .Select(part => part?.Trim())
                .Where(part => !string.IsNullOrEmpty(part)));

            return (sender.Id, senderName);
        }
    }
}
