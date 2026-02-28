using CalendarApp.Services.Auth;
using CalendarApp.Services.Messages;
using CalendarApp.Services.UserPresence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace CalendarApp.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IMessageService messageService;
        private readonly IUserPresenceTracker presenceTracker;
        private readonly IAuthenticationService authenticationService;

        public ChatHub(
            IMessageService messageService,
            IUserPresenceTracker presenceTracker,
            IAuthenticationService authenticationService)
        {
            this.messageService = messageService;
            this.presenceTracker = presenceTracker;
            this.authenticationService = authenticationService;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

            try
            {
                var userId = authenticationService.GetCurrentUserId(Context.User ?? throw new InvalidOperationException("Потребителят не е намерен."));
                var becameOnline = await presenceTracker.UserConnectedAsync(userId, Context.ConnectionId);

                if (becameOnline)
                {
                    await Clients.Others.SendAsync("PresenceChanged", new
                    {
                        userId,
                        isOnline = true
                    });
                }
            }
            catch (InvalidOperationException)
            {
            }
            catch (FormatException)
            {
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var userId = authenticationService.GetCurrentUserId(Context.User ?? throw new InvalidOperationException("Потребителят не е намерен."));
                var becameOffline = await presenceTracker.UserDisconnectedAsync(userId, Context.ConnectionId);

                if (becameOffline)
                {
                    await Clients.Others.SendAsync("PresenceChanged", new
                    {
                        userId,
                        isOnline = false
                    });
                }
            }
            catch (InvalidOperationException)
            {
                // Ignore presence updates if the user identifier is not available.
            }
            catch (FormatException)
            {
                // Ignore presence updates if the user identifier is not available.
            }
            finally
            {
                await base.OnDisconnectedAsync(exception);
            }
        }

        public async Task JoinFriendship(Guid friendshipId)
        {
            var userId = authenticationService.GetCurrentUserId(Context.User ?? throw new InvalidOperationException("Потребителят не е намерен."));

            try
            {
                await messageService.EnsureFriendshipAccessAsync(userId, friendshipId);
            }
            catch (InvalidOperationException)
            {
                throw new HubException("Чатът не е намерен или достъпът е отказан.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, BuildFriendshipGroupName(friendshipId));
        }

        public async Task LeaveFriendship(Guid friendshipId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, BuildFriendshipGroupName(friendshipId));
        }

        public async Task SendMessage(Guid friendshipId, string message)
        {
            var trimmedMessage = message?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedMessage))
            {
                return;
            }

            var userId = authenticationService.GetCurrentUserId(Context.User ?? throw new InvalidOperationException("Потребителят не е намерен."));
            try
            {
                var chatMessage = await messageService.SaveMessageAsync(userId, friendshipId, trimmedMessage);

                await Clients.Group(BuildFriendshipGroupName(friendshipId))
                    .SendAsync("ReceiveMessage", chatMessage);
            }
            catch (InvalidOperationException)
            {
                throw new HubException("Чатът не е намерен или достъпът е отказан.");
            }
            catch (ArgumentException)
            {
                return;
            }
        }

        public async Task JoinMeeting(Guid meetingId)
        {
            var userId = authenticationService.GetCurrentUserId(Context.User ?? throw new InvalidOperationException("Потребителят не е намерен."));

            try
            {
                await messageService.EnsureMeetingAccessAsync(userId, meetingId);
            }
            catch (InvalidOperationException)
            {
                throw new HubException("Събитието не е намерено или достъпът е отказан.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, BuildMeetingGroupName(meetingId));
        }

        public Task LeaveMeeting(Guid meetingId)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, BuildMeetingGroupName(meetingId));
        }

        public async Task SendMeetingMessage(Guid meetingId, string message)
        {
            var trimmedMessage = message?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedMessage))
            {
                return;
            }

            var userId = authenticationService.GetCurrentUserId(Context.User ?? throw new InvalidOperationException("Потребителят не е намерен."));
            try
            {
                var chatMessage = await messageService.SaveMeetingMessageAsync(userId, meetingId, trimmedMessage);

                await Clients.Group(BuildMeetingGroupName(meetingId))
                    .SendAsync("ReceiveMessage", chatMessage);
            }
            catch (InvalidOperationException)
            {
                throw new HubException("Събитието не е намерено или достъпът е отказан.");
            }
            catch (ArgumentException)
            {
                return;
            }
        }

        private static string BuildFriendshipGroupName(Guid friendshipId) => $"friendship:{friendshipId}";

        private static string BuildMeetingGroupName(Guid meetingId) => $"meeting:{meetingId}";
    }
}
