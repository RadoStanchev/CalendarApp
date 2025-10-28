using CalendarApp.Data;
using CalendarApp.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CalendarApp.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<Contact> userManager;

        public ChatHub(ApplicationDbContext db, UserManager<Contact> userManager)
        {
            this.db = db;
            this.userManager = userManager;
        }

        public async Task JoinFriendship(Guid friendshipId)
        {
            var friendship = await GetFriendshipAsync(friendshipId);
            if (friendship == null)
            {
                throw new HubException("Чатът не е намерен или достъпът е отказан.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, BuildGroupName(friendshipId));
        }

        public async Task LeaveFriendship(Guid friendshipId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, BuildGroupName(friendshipId));
        }

        public async Task SendMessage(Guid friendshipId, string message)
        {
            var trimmedMessage = message?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedMessage))
            {
                return;
            }

            var friendship = await GetFriendshipAsync(friendshipId);
            if (friendship == null)
            {
                throw new HubException("Чатът не е намерен или достъпът е отказан.");
            }

            var userId = GetCurrentUserId();
            var sender = await userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.Id, u.FirstName, u.LastName })
                .AsNoTracking()
                .FirstAsync();

            var entity = new Message
            {
                FriendshipId = friendshipId,
                SenderId = userId,
                Content = trimmedMessage,
                SentAt = DateTime.UtcNow
            };

            db.Messages.Add(entity);
            await db.SaveChangesAsync();

            await Clients.Group(BuildGroupName(friendshipId)).SendAsync("ReceiveMessage", new
            {
                friendshipId,
                messageId = entity.Id,
                senderId = sender.Id,
                senderName = BuildFullName(sender.FirstName, sender.LastName),
                content = entity.Content,
                sentAt = entity.SentAt
            });
        }

        private async Task<Friendship?> GetFriendshipAsync(Guid friendshipId)
        {
            var userId = GetCurrentUserId();

            return await db.Friendships
                .Where(f => f.Id == friendshipId
                            && f.Status == FriendshipStatus.Accepted
                            && (f.RequesterId == userId || f.ReceiverId == userId))
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        private Guid GetCurrentUserId()
        {
            var identifier = Context.UserIdentifier;
            if (string.IsNullOrWhiteSpace(identifier) || !Guid.TryParse(identifier, out var userId))
            {
                throw new HubException("Потребителят не е автентикиран.");
            }

            return userId;
        }

        private static string BuildGroupName(Guid friendshipId) => $"friendship:{friendshipId}";

        private static string BuildFullName(string? firstName, string? lastName)
        {
            var first = firstName?.Trim() ?? string.Empty;
            var last = lastName?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(first) && string.IsNullOrEmpty(last))
            {
                return string.Empty;
            }

            return string.Join(" ", new[] { first, last }.Where(part => !string.IsNullOrEmpty(part)));
        }
    }
}
