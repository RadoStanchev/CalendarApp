using System;
using System.Security.Claims;

namespace CalendarApp.Infrastructure.Extensions
{
    public static class HubExtensions
    {
        public static Guid GetUserIdGuid(this ClaimsPrincipal user)
        {
            var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(id);
        }
    }
}