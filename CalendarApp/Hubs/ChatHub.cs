using Microsoft.AspNetCore.SignalR;

namespace CalendarApp.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            var safeUser = string.IsNullOrWhiteSpace(user) ? "Anonymous" : user;
            var safeMessage = message.Trim();

            await Clients.All.SendAsync("ReceiveMessage", safeUser, safeMessage);
        }
    }
}
