using CalendarApp.Data;
using CalendarApp.Data.Models;
using CalendarApp.Models.Chat;
using CalendarApp.Services.Messages;
using CalendarApp.Services.MessageSeens;
using CalendarApp.Services.UserPresence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

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
        private readonly IMessageService messageService;
        private readonly IMessageSeenService messageSeenService;
        private readonly IUserPresenceTracker presenceTracker;

        public ChatController(ApplicationDbContext db, UserManager<Contact> userManager, IMessageService messageService, IMessageSeenService messageSeenService, IUserPresenceTracker presenceTracker)
        {
            this.db = db;
            this.userManager = userManager;
            this.messageService = messageService;
            this.messageSeenService = messageSeenService;
            this.presenceTracker = presenceTracker;
        }

        [HttpGet]
        public async Task<IActionResult> Index(Guid? friendshipId, Guid? meetingId, ThreadType? type)
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

            var onlineUsers = await presenceTracker.GetOnlineUsersAsync();
            var onlineUserSet = new HashSet<Guid>(onlineUsers);

            var friendshipThreads = friendships
                .Select(f =>
                {
                    latestMessageLookup.TryGetValue(f.Id, out var lastMessage);
                    var isOnline = onlineUserSet.Contains(f.FriendId);

                    return new ChatThreadViewModel
                    {
                        ThreadId = f.Id,
                        Type = ThreadType.Friendship,
                        FriendshipId = f.Id,
                        FriendId = f.FriendId,
                        DisplayName = BuildFullName(f.FriendFirstName, f.FriendLastName),
                        SecondaryLabel = f.FriendEmail ?? string.Empty,
                        AvatarInitials = BuildInitials(f.FriendFirstName, f.FriendLastName),
                        AccentClass = GetAccentClass(f.FriendId),
                        LastMessagePreview = lastMessage?.Content ?? string.Empty,
                        LastMessageAt = lastMessage?.SentAt,
                        LastActivityLabel = BuildActivityLabel(lastMessage?.SentAt),
                        IsOnline = isOnline,
                        CreatedAt = f.CreatedAt
                    };
                })
                .OrderByDescending(t => t.IsOnline)
                .ThenByDescending(t => t.LastMessageAt ?? t.CreatedAt)
                .ToList();

            var meetings = await db.Meetings
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
                .AsNoTracking()
                .ToListAsync();

            var meetingIds = meetings.Select(m => m.Id).ToList();

            var latestMeetingMessages = await db.Messages
                .Where(m => m.MeetingId != null && meetingIds.Contains(m.MeetingId.Value))
                .OrderByDescending(m => m.SentAt)
                .Select(m => new
                {
                    MeetingId = m.MeetingId!.Value,
                    m.Content,
                    m.SentAt
                })
                .AsNoTracking()
                .ToListAsync();

            var latestMeetingLookup = latestMeetingMessages
                .GroupBy(m => m.MeetingId)
                .ToDictionary(g => g.Key, g => g.First());

            var meetingThreads = meetings
                .Select(m =>
                {
                    latestMeetingLookup.TryGetValue(m.Id, out var lastMessage);
                    var title = BuildMeetingTitle(m.Description, m.StartTime);
                    var subtitle = BuildMeetingSubtitle(m.StartTime, m.Location);

                    return new ChatThreadViewModel
                    {
                        ThreadId = m.Id,
                        Type = ThreadType.Meeting,
                        DisplayName = title,
                        SecondaryLabel = subtitle,
                        AvatarInitials = BuildInitials(title, null),
                        AccentClass = GetAccentClass(m.Id),
                        LastMessagePreview = lastMessage?.Content ?? string.Empty,
                        LastMessageAt = lastMessage?.SentAt,
                        LastActivityLabel = BuildActivityLabel(lastMessage?.SentAt),
                        CreatedAt = m.StartTime,
                        Meeting = new MeetingThreadMetadata
                        {
                            MeetingId = m.Id,
                            Title = title,
                            StartTimeUtc = DateTime.SpecifyKind(m.StartTime, DateTimeKind.Utc),
                            Location = m.Location,
                            IsOrganizer = m.CreatedById == userId,
                            ParticipantCount = m.ParticipantCount
                        }
                    };
                })
                .OrderByDescending(t => t.LastMessageAt ?? t.CreatedAt)
                .ToList();

            ThreadType? activeThreadType = type;
            Guid? activeThreadId = null;

            if (type == ThreadType.Meeting && meetingId.HasValue)
            {
                activeThreadId = meetingId.Value;
            }
            else if (type == ThreadType.Friendship && friendshipId.HasValue)
            {
                activeThreadId = friendshipId.Value;
            }
            else if (friendshipId.HasValue)
            {
                activeThreadType = ThreadType.Friendship;
                activeThreadId = friendshipId.Value;
            }
            else if (meetingId.HasValue)
            {
                activeThreadType = ThreadType.Meeting;
                activeThreadId = meetingId.Value;
            }

            if (!activeThreadType.HasValue)
            {
                var firstFriendship = friendshipThreads.FirstOrDefault();
                if (firstFriendship != null)
                {
                    activeThreadType = ThreadType.Friendship;
                    activeThreadId = firstFriendship.ThreadId;
                }
                else
                {
                    var firstMeeting = meetingThreads.FirstOrDefault();
                    if (firstMeeting != null)
                    {
                        activeThreadType = ThreadType.Meeting;
                        activeThreadId = firstMeeting.ThreadId;
                    }
                }
            }

            IReadOnlyList<ChatMessageViewModel> messages = Array.Empty<ChatMessageViewModel>();

            if (activeThreadType == ThreadType.Friendship && activeThreadId.HasValue)
            {
                var messageItems = await db.Messages
                    .Where(m => m.FriendshipId == activeThreadId.Value)
                    .OrderByDescending(m => m.SentAt)
                    .Take(MessagesPageSize)
                    .Select(m => new ChatMessageViewModel
                    {
                        Id = m.Id,
                        SenderId = m.SenderId,
                        SenderName = BuildFullName(m.Sender.FirstName, m.Sender.LastName),
                        Content = m.Content,
                        SentAt = m.SentAt,
                        FriendshipId = m.FriendshipId,
                        MeetingId = m.MeetingId
                    })
                    .AsNoTracking()
                    .ToListAsync();

                messages = messageItems
                    .OrderBy(m => m.SentAt)
                    .ToList();
            }
            else if (activeThreadType == ThreadType.Meeting && activeThreadId.HasValue)
            {
                var messageItems = await db.Messages
                    .Where(m => m.MeetingId == activeThreadId.Value)
                    .OrderByDescending(m => m.SentAt)
                    .Take(MessagesPageSize)
                    .Select(m => new ChatMessageViewModel
                    {
                        Id = m.Id,
                        SenderId = m.SenderId,
                        SenderName = BuildFullName(m.Sender.FirstName, m.Sender.LastName),
                        Content = m.Content,
                        SentAt = m.SentAt,
                        FriendshipId = m.FriendshipId,
                        MeetingId = m.MeetingId
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
                FriendshipThreads = friendshipThreads,
                MeetingThreads = meetingThreads,
                ActiveThreadType = activeThreadType,
                ActiveThreadId = activeThreadId,
                Messages = messages
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Thread(Guid id, ThreadType type)
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var userId = currentUser.Id;

            if (type == ThreadType.Meeting)
            {
                return await BuildMeetingThreadResponse(id, userId);
            }

            return await BuildFriendshipThreadResponse(id, userId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkThreadAsRead(Guid threadId, ThreadType threadType)
        {
            if (threadId == Guid.Empty)
            {
                return BadRequest();
            }

            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            try
            {
                if (threadType == ThreadType.Meeting)
                {
                    await messageSeenService.MarkMeetingMessagesAsSeenAsync(currentUser.Id, threadId, HttpContext.RequestAborted);
                }
                else
                {
                    await messageSeenService.MarkFriendshipMessagesAsSeenAsync(currentUser.Id, threadId, HttpContext.RequestAborted);
                }
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }

            return NoContent();
        }

        private async Task<IActionResult> BuildFriendshipThreadResponse(Guid friendshipId, Guid userId)
        {
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
                    SentAt = m.SentAt,
                    FriendshipId = m.FriendshipId,
                    MeetingId = m.MeetingId
                })
                .AsNoTracking()
                .ToListAsync();

            var messages = messageItems
                .OrderBy(m => m.SentAt)
                .ToList();

            var isOnline = await presenceTracker.IsOnlineAsync(friendship.FriendId);

            var response = new
            {
                threadId = friendship.Id,
                threadType = ThreadType.Friendship.ToString().ToLowerInvariant(),
                friendshipId = friendship.Id,
                displayName = BuildFullName(friendship.FriendFirstName, friendship.FriendLastName),
                secondaryLabel = friendship.FriendEmail ?? string.Empty,
                avatar = BuildInitials(friendship.FriendFirstName, friendship.FriendLastName),
                accent = GetAccentClass(friendship.FriendId),
                lastActivity = BuildActivityLabel(messages.LastOrDefault()?.SentAt),
                isOnline,
                messages = messages.Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    senderName = m.SenderName,
                    m.Content,
                    m.SentAt,
                    m.FriendshipId,
                    m.MeetingId
                })
            };

            return Json(response);
        }

        private async Task<IActionResult> BuildMeetingThreadResponse(Guid meetingId, Guid userId)
        {
            var meeting = await db.Meetings
                .Where(m => m.Id == meetingId
                            && (m.CreatedById == userId
                                || m.Participants.Any(p => p.ContactId == userId && p.Status == ParticipantStatus.Accepted)))
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
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (meeting == null)
            {
                return NotFound();
            }

            var messageItems = await db.Messages
                .Where(m => m.MeetingId == meeting.Id)
                .OrderByDescending(m => m.SentAt)
                .Take(MessagesPageSize)
                .Select(m => new ChatMessageViewModel
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    SenderName = BuildFullName(m.Sender.FirstName, m.Sender.LastName),
                    Content = m.Content,
                    SentAt = m.SentAt,
                    FriendshipId = m.FriendshipId,
                    MeetingId = m.MeetingId
                })
                .AsNoTracking()
                .ToListAsync();

            var messages = messageItems
                .OrderBy(m => m.SentAt)
                .ToList();

            var title = BuildMeetingTitle(meeting.Description, meeting.StartTime);
            var subtitle = BuildMeetingSubtitle(meeting.StartTime, meeting.Location);

            var response = new
            {
                threadId = meeting.Id,
                threadType = ThreadType.Meeting.ToString().ToLowerInvariant(),
                meetingId = meeting.Id,
                isOnline = false,
                metadata = new
                {
                    meetingId = meeting.Id,
                    title,
                    subtitle,
                    startTimeUtc = DateTime.SpecifyKind(meeting.StartTime, DateTimeKind.Utc),
                    location = meeting.Location,
                    isOrganizer = meeting.CreatedById == userId,
                    participantCount = meeting.ParticipantCount
                },
                displayName = title,
                secondaryLabel = subtitle,
                avatar = BuildInitials(title, null),
                accent = GetAccentClass(meeting.Id),
                lastActivity = BuildActivityLabel(messages.LastOrDefault()?.SentAt),
                messages = messages.Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    senderName = m.SenderName,
                    m.Content,
                    m.SentAt,
                    m.FriendshipId,
                    m.MeetingId
                })
            };

            return Json(response);
        }

        private static string GetAccentClass(Guid key)
        {
            var index = Math.Abs(key.GetHashCode()) % AccentPalette.Length;
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

        private static string BuildMeetingTitle(string? description, DateTime startTimeUtc)
        {
            if (!string.IsNullOrWhiteSpace(description))
            {
                return description.Trim();
            }

            var local = DateTime.SpecifyKind(startTimeUtc, DateTimeKind.Utc).ToLocalTime();
            return $"Събитие на {local:dd MMM yyyy}";
        }

        private static string BuildMeetingSubtitle(DateTime startTimeUtc, string? location)
        {
            var local = DateTime.SpecifyKind(startTimeUtc, DateTimeKind.Utc).ToLocalTime();
            var formatted = local.ToString("g", BulgarianCulture);

            if (string.IsNullOrWhiteSpace(location))
            {
                return formatted;
            }

            return $"{formatted} • {location.Trim()}";
        }
    }
}
