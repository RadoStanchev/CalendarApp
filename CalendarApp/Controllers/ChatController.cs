using System.Globalization;
using System.Linq;
using System.Text;
using CalendarApp.Data;
using CalendarApp.Data.Models;
using CalendarApp.Models.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CalendarApp.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private const int MessagesPageSize = 100;

        private static readonly string[] AccentPalette =
        [
            "accent-blue",
            "accent-purple",
            "accent-green",
            "accent-orange",
            "accent-teal"
        ];

        private static readonly CultureInfo BulgarianCulture = CultureInfo.GetCultureInfo("bg-BG");

        private readonly ApplicationDbContext db;
        private readonly UserManager<Contact> userManager;

        public ChatController(ApplicationDbContext db, UserManager<Contact> userManager)
        {
            this.db = db;
            this.userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(Guid? friendshipId)
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var userId = currentUser.Id;

            var friendships = await db.Friendships
                .Where(f => f.Status == FriendshipStatus.Accepted
                            && (f.RequesterId == userId || f.ReceiverId == userId))
                .Select(f => new
                {
                    f.Id,
                    f.CreatedAt,
                    FriendId = f.RequesterId == userId ? f.ReceiverId : f.RequesterId,
                    FriendFirstName = f.RequesterId == userId ? f.Receiver.FirstName : f.Requester.FirstName,
                    FriendLastName = f.RequesterId == userId ? f.Receiver.LastName : f.Requester.LastName,
                    FriendEmail = f.RequesterId == userId ? f.Receiver.Email : f.Requester.Email
                })
                .AsNoTracking()
                .ToListAsync();

            var friendshipIds = friendships.Select(f => f.Id).ToList();

            var latestMessages = await db.Messages
                .Where(m => m.FriendshipId != null && friendshipIds.Contains(m.FriendshipId.Value))
                .OrderByDescending(m => m.SentAt)
                .Select(m => new
                {
                    FriendshipId = m.FriendshipId!.Value,
                    m.Content,
                    m.SentAt
                })
                .AsNoTracking()
                .ToListAsync();

            var latestMessageLookup = latestMessages
                .GroupBy(m => m.FriendshipId)
                .ToDictionary(g => g.Key, g => g.First());

            var threads = friendships
                .Select(f =>
                {
                    latestMessageLookup.TryGetValue(f.Id, out var lastMessage);

                    return new ChatThreadViewModel
                    {
                        FriendshipId = f.Id,
                        FriendId = f.FriendId,
                        FriendName = BuildFullName(f.FriendFirstName, f.FriendLastName),
                        FriendEmail = f.FriendEmail ?? string.Empty,
                        AvatarInitials = BuildInitials(f.FriendFirstName, f.FriendLastName),
                        AccentClass = GetAccentClass(f.FriendId),
                        LastMessagePreview = lastMessage?.Content ?? string.Empty,
                        LastMessageAt = lastMessage?.SentAt,
                        LastActivityLabel = BuildActivityLabel(lastMessage?.SentAt),
                        CreatedAt = f.CreatedAt
                    };
                })
                .OrderByDescending(t => t.LastMessageAt ?? t.CreatedAt)
                .ToList();

            var activeFriendshipId = friendshipId ?? threads.FirstOrDefault()?.FriendshipId;
            IReadOnlyList<ChatMessageViewModel> messages = Array.Empty<ChatMessageViewModel>();

            if (activeFriendshipId.HasValue)
            {
                var messageItems = await db.Messages
                    .Where(m => m.FriendshipId == activeFriendshipId.Value)
                    .OrderByDescending(m => m.SentAt)
                    .Take(MessagesPageSize)
                    .Select(m => new ChatMessageViewModel
                    {
                        Id = m.Id,
                        SenderId = m.SenderId,
                        SenderName = BuildFullName(m.Sender.FirstName, m.Sender.LastName),
                        Content = m.Content,
                        SentAt = m.SentAt
                    })
                    .AsNoTracking()
                    .ToListAsync();

                messages = messageItems
                    .OrderBy(m => m.SentAt)
                    .ToList();
            }

            var model = new ChatViewModel
            {
                CurrentUserId = userId,
                CurrentUserName = BuildFullName(currentUser.FirstName, currentUser.LastName),
                Threads = threads,
                ActiveFriendshipId = activeFriendshipId,
                Messages = messages
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Thread(Guid friendshipId)
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var userId = currentUser.Id;

            var friendship = await db.Friendships
                .Where(f => f.Id == friendshipId
                            && f.Status == FriendshipStatus.Accepted
                            && (f.RequesterId == userId || f.ReceiverId == userId))
                .Select(f => new
                {
                    f.Id,
                    FriendId = f.RequesterId == userId ? f.ReceiverId : f.RequesterId,
                    FriendFirstName = f.RequesterId == userId ? f.Receiver.FirstName : f.Requester.FirstName,
                    FriendLastName = f.RequesterId == userId ? f.Receiver.LastName : f.Requester.LastName,
                    FriendEmail = f.RequesterId == userId ? f.Receiver.Email : f.Requester.Email
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (friendship == null)
            {
                return NotFound();
            }

            var messageItems = await db.Messages
                .Where(m => m.FriendshipId == friendship.Id)
                .OrderByDescending(m => m.SentAt)
                .Take(MessagesPageSize)
                .Select(m => new ChatMessageViewModel
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    SenderName = BuildFullName(m.Sender.FirstName, m.Sender.LastName),
                    Content = m.Content,
                    SentAt = m.SentAt
                })
                .AsNoTracking()
                .ToListAsync();

            var messages = messageItems
                .OrderBy(m => m.SentAt)
                .ToList();

            var response = new
            {
                friendshipId = friendship.Id,
                friendId = friendship.FriendId,
                friendName = BuildFullName(friendship.FriendFirstName, friendship.FriendLastName),
                friendEmail = friendship.FriendEmail ?? string.Empty,
                avatar = BuildInitials(friendship.FriendFirstName, friendship.FriendLastName),
                accent = GetAccentClass(friendship.FriendId),
                lastActivity = BuildActivityLabel(messages.LastOrDefault()?.SentAt),
                messages = messages.Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    senderName = m.SenderName,
                    m.Content,
                    m.SentAt
                })
            };

            return Json(response);
        }

        private static string GetAccentClass(Guid friendId)
        {
            var index = Math.Abs(friendId.GetHashCode()) % AccentPalette.Length;
            return AccentPalette[index];
        }

        private static string BuildInitials(string? firstName, string? lastName)
        {
            var builder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(firstName))
            {
                builder.Append(char.ToUpper(firstName![0], BulgarianCulture));
            }

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                builder.Append(char.ToUpper(lastName![0], BulgarianCulture));
            }

            return builder.Length == 0 ? "?" : builder.ToString();
        }

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

        private static string BuildActivityLabel(DateTime? sentAtUtc)
        {
            if (!sentAtUtc.HasValue)
            {
                return "Няма изпратени съобщения";
            }

            var localTime = DateTime.SpecifyKind(sentAtUtc.Value, DateTimeKind.Utc).ToLocalTime();
            return $"Последно съобщение: {localTime.ToString("g", BulgarianCulture)}";
        }
    }
}
