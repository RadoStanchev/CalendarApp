using CalendarApp.Data;
using CalendarApp.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CalendarApp.Services.Messages
{
    public class MessageSeenService : IMessageSeenService
    {
        private readonly ApplicationDbContext db;
        private readonly IMessageService messageService;

        public MessageSeenService(ApplicationDbContext db, IMessageService messageService)
        {
            this.db = db;
            this.messageService = messageService;
        }

        public async Task MarkFriendshipMessagesAsSeenAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken = default)
        {
            await messageService.EnsureFriendshipAccessAsync(userId, friendshipId, cancellationToken);

            var now = DateTime.UtcNow;

            var unseenMessageIds = await db.Messages
                .Where(m => m.FriendshipId == friendshipId && m.SenderId != userId)
                .Where(m => !db.MessageSeens.Any(r => r.MessageId == m.Id && r.ContactId == userId))
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            if (unseenMessageIds.Count == 0)
            {
                return;
            }

            var receipts = unseenMessageIds
                .Select(messageId => new MessageSeen
                {
                    MessageId = messageId,
                    ContactId = userId,
                    SeenAt = now
                })
                .ToList();

            db.MessageSeens.AddRange(receipts);
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task MarkMeetingMessagesAsSeenAsync(Guid userId, Guid meetingId, CancellationToken cancellationToken = default)
        {
            await messageService.EnsureMeetingAccessAsync(userId, meetingId, cancellationToken);

            var now = DateTime.UtcNow;

            var unseenMessageIds = await db.Messages
                .Where(m => m.MeetingId == meetingId && m.SenderId != userId)
                .Where(m => !db.MessageSeens.Any(r => r.MessageId == m.Id && r.ContactId == userId))
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            if (unseenMessageIds.Count == 0)
            {
                return;
            }

            var receipts = unseenMessageIds
                .Select(messageId => new MessageSeen
                {
                    MessageId = messageId,
                    ContactId = userId,
                    SeenAt = now
                })
                .ToList();

            db.MessageSeens.AddRange(receipts);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
