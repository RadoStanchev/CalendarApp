using AutoMapper;
using CalendarApp.Services.Auth;
using CalendarApp.Models.Chat;
using CalendarApp.Services.Friendships;
using CalendarApp.Services.Meetings;
using CalendarApp.Services.Messages;
using CalendarApp.Services.MessageSeens;
using CalendarApp.Services.User;
using CalendarApp.Services.UserPresence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CalendarApp.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private const int MessagesPageSize = 100;

        private readonly IAuthenticationService authenticationService;
        private readonly IFriendshipService friendshipService;
        private readonly IMeetingService meetingService;
        private readonly IMessageService messageService;
        private readonly IMessageSeenService messageSeenService;
        private readonly IUserPresenceTracker presenceTracker;
        private readonly IUserService userService;
        private readonly IMapper mapper;

        public ChatController(IAuthenticationService authenticationService, IFriendshipService friendshipService, IMeetingService meetingService, IMessageService messageService, IMessageSeenService messageSeenService, IUserPresenceTracker presenceTracker, IUserService userService, IMapper mapper)
        {
            this.authenticationService = authenticationService;
            this.friendshipService = friendshipService;
            this.meetingService = meetingService;
            this.messageService = messageService;
            this.messageSeenService = messageSeenService;
            this.presenceTracker = presenceTracker;
            this.userService = userService;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> Index(Guid? friendshipId, Guid? meetingId, ThreadType? type)
        {
            var userId = authenticationService.GetCurrentUserId(User);
            var currentUser = await userService.GetByIdAsync(userId);
            if (currentUser == null)
            {
                return Challenge();
            }

            var friendshipThreadDtos = await friendshipService.GetChatThreadsAsync(userId);

            var onlineUsers = await presenceTracker.GetOnlineUsersAsync();
            var onlineUserSet = new HashSet<Guid>(onlineUsers);

            var friendshipThreads = mapper.Map<List<ChatThreadViewModel>>(friendshipThreadDtos);

            foreach (var thread in friendshipThreads)
            {
                if (thread.FriendId.HasValue)
                {
                    thread.IsOnline = onlineUserSet.Contains(thread.FriendId.Value);
                }
            }

            friendshipThreads = friendshipThreads
                .OrderByDescending(t => t.IsOnline)
                .ThenByDescending(t => t.LastMessageAt ?? t.CreatedAt)
                .ToList();

            var meetingThreadDtos = await meetingService.GetChatThreadsAsync(userId);

            var meetingThreads = mapper.Map<List<ChatThreadViewModel>>(meetingThreadDtos, opts =>
                opts.Items["CurrentUserId"] = userId)
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
                var messageItems = await messageService.GetRecentFriendshipMessagesAsync(userId, activeThreadId.Value, MessagesPageSize);

                messages = mapper.Map<List<ChatMessageViewModel>>(messageItems);
            }
            else if (activeThreadType == ThreadType.Meeting && activeThreadId.HasValue)
            {
                var messageItems = await messageService.GetRecentMeetingMessagesAsync(userId, activeThreadId.Value, MessagesPageSize);

                messages = mapper.Map<List<ChatMessageViewModel>>(messageItems);
            }

            var model = new ChatViewModel
            {
                CurrentUserId = userId,
                CurrentUserName = $"{currentUser.FirstName} {currentUser.LastName}",
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
            var userId = authenticationService.GetCurrentUserId(User);

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

            var userId = authenticationService.GetCurrentUserId(User);

            try
            {
                if (threadType == ThreadType.Meeting)
                {
                    await messageSeenService.MarkMeetingMessagesAsSeenAsync(userId, threadId);
                }
                else
                {
                    await messageSeenService.MarkFriendshipMessagesAsSeenAsync(userId, threadId);
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
            var friendship = await friendshipService.GetChatThreadAsync(friendshipId, userId);

            if (friendship == null)
            {
                return NotFound();
            }

            var messageItems = await messageService.GetRecentFriendshipMessagesAsync(userId, friendshipId, MessagesPageSize);

            var messages = mapper.Map<List<ChatMessageViewModel>>(messageItems);

            var thread = mapper.Map<ChatThreadViewModel>(friendship);
            thread.IsOnline = await presenceTracker.IsOnlineAsync(friendship.FriendId);

            var response = new
            {
                threadId = thread.ThreadId,
                threadType = ThreadType.Friendship.ToString().ToLowerInvariant(),
                friendshipId = thread.FriendshipId,
                displayName = thread.DisplayName,
                avatar = thread.AvatarInitials,
                accent = thread.AccentClass,
                lastActivity = ChatViewModelHelper.BuildActivityLabel(messages.LastOrDefault()?.SentAt),
                isOnline = thread.IsOnline,
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
            var meeting = await meetingService.GetChatThreadAsync(meetingId, userId);

            if (meeting == null)
            {
                return NotFound();
            }

            var messageItems = await messageService.GetRecentMeetingMessagesAsync(userId, meetingId, MessagesPageSize);

            var messages = mapper.Map<List<ChatMessageViewModel>>(messageItems);

            var thread = mapper.Map<ChatThreadViewModel>(meeting, opts =>
                opts.Items["CurrentUserId"] = userId);

            var response = new
            {
                threadId = thread.ThreadId,
                threadType = ThreadType.Meeting.ToString().ToLowerInvariant(),
                meetingId = thread.Meeting?.MeetingId,
                isOnline = false,
                metadata = thread.Meeting,
                displayName = thread.DisplayName,
                avatar = thread.AvatarInitials,
                accent = thread.AccentClass,
                lastActivity = ChatViewModelHelper.BuildActivityLabel(messages.LastOrDefault()?.SentAt),
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

    }
}
