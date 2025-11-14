using CalendarApp.Data.Models;
using CalendarApp.Infrastructure.Extensions;
using CalendarApp.Infrastructure.Extentions;
using CalendarApp.Services.Messages;
using CalendarApp.Services.UserPresence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<Contact> userManager;

        public ChatHub(IMessageService messageService, IUserPresenceTracker presenceTracker)
        {
            this.messageService = messageService;
            this.presenceTracker = presenceTracker;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

            try
            {
                var userId = Context.User.GetUserIdGuid();
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
            catch (HubException)
            {
                
            }
        }


        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var userId = Context.User.GetUserIdGuid();
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
            catch (HubException)
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
            var userId = Context.User.GetUserIdGuid();

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

            var userId = Context.User.GetUserIdGuid();
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
            var userId = Context.User.GetUserIdGuid();

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

            var userId = Context.User.GetUserIdGuid();
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
