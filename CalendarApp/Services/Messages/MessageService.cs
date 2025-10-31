using CalendarApp.Data;
using CalendarApp.Data.Models;
using CalendarApp.Services.Messages.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

        public string BuildGroupName(Guid friendshipId) => $"friendship:{friendshipId}";

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

        public async Task<ChatMessageDto> SaveMessageAsync(Guid userId, Guid friendshipId, string content, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Message content must not be empty.", nameof(content));
            }

            await EnsureFriendshipAccessAsync(userId, friendshipId, cancellationToken);

            var sender = await userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.Id, u.FirstName, u.LastName })
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (sender == null)
            {
                throw new InvalidOperationException("Sender not found.");
            }

            var entity = new Message
            {
                FriendshipId = friendshipId,
                SenderId = userId,
                Content = content,
                SentAt = DateTime.UtcNow
            };

            db.Messages.Add(entity);
            await db.SaveChangesAsync(cancellationToken);

            var senderName = string.Join(" ", new[] { sender.FirstName, sender.LastName }
                .Select(part => part?.Trim())
                .Where(part => !string.IsNullOrEmpty(part)));

            return new ChatMessageDto
            {
                FriendshipId = friendshipId,
                MessageId = entity.Id,
                SenderId = sender.Id,
                SenderName = senderName,
                Content = entity.Content,
                SentAt = entity.SentAt
            };
        }
    }
}
