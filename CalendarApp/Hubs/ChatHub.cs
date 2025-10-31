using CalendarApp.Services.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;

namespace CalendarApp.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IMessageService messageService;

        public ChatHub(IMessageService messageService)
        {
            this.messageService = messageService;
        }

        public async Task JoinFriendship(Guid friendshipId)
        {
            var userId = GetCurrentUserId();

            try
            {
                await messageService.EnsureFriendshipAccessAsync(userId, friendshipId);
            }
            catch (InvalidOperationException)
            {
                throw new HubException("Чатът не е намерен или достъпът е отказан.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, messageService.BuildGroupName(friendshipId));
        }

        public async Task LeaveFriendship(Guid friendshipId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, messageService.BuildGroupName(friendshipId));
        }

        public async Task SendMessage(Guid friendshipId, string message)
        {
            var trimmedMessage = message?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedMessage))
            {
                return;
            }

            var userId = GetCurrentUserId();
            try
            {
                var chatMessage = await messageService.SaveMessageAsync(userId, friendshipId, trimmedMessage);

                await Clients.Group(messageService.BuildGroupName(friendshipId))
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

        private Guid GetCurrentUserId()
        {
            var identifier = Context.UserIdentifier;
            if (string.IsNullOrWhiteSpace(identifier) || !Guid.TryParse(identifier, out var userId))
            {
                throw new HubException("Потребителят не е автентикиран.");
            }

            return userId;
        }

    }
}
