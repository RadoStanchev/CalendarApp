using CalendarApp.Data;
using CalendarApp.Data.Models;
using CalendarApp.Services.Messages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarApp.Services.MessageSeens
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

        public async Task MarkFriendshipMessagesAsSeenAsync(Guid userId, Guid friendshipId)
        {
            await messageService.EnsureFriendshipAccessAsync(userId, friendshipId);

            var now = DateTime.UtcNow;

            var unseenMessageIds = await db.Messages
                .Where(m => m.FriendshipId == friendshipId && m.SenderId != userId)
                .Where(m => !db.MessageSeens.Any(r => r.MessageId == m.Id && r.ContactId == userId))
                .Select(m => m.Id)
                .ToListAsync();

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
            await db.SaveChangesAsync();
        }

        public async Task MarkMeetingMessagesAsSeenAsync(Guid userId, Guid meetingId)
        {
            await messageService.EnsureMeetingAccessAsync(userId, meetingId);

            var now = DateTime.UtcNow;

            var unseenMessageIds = await db.Messages
                .Where(m => m.MeetingId == meetingId && m.SenderId != userId)
                .Where(m => !db.MessageSeens.Any(r => r.MessageId == m.Id && r.ContactId == userId))
                .Select(m => m.Id)
                .ToListAsync();

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
            await db.SaveChangesAsync();
        }
    }
}
