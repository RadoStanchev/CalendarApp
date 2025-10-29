using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CalendarApp.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
    }
}
