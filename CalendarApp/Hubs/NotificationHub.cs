using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CalendarApp.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            if (!Guid.TryParse(Context.UserIdentifier, out _))
            {
                throw new HubException("User is not authenticated.");
            }

            return base.OnConnectedAsync();
        }
    }
}
