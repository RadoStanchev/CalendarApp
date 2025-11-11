using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace CalendarApp.Infrastructure.Extentions
{
    public static class UserManagerExtensions
    {
        public static Guid GetUserIdGuid<TUser>(this UserManager<TUser> userManager, ClaimsPrincipal principal)
            where TUser : class
        {
            var userId = userManager.GetUserId(principal);
            return Guid.Parse(userId);
        }
    }
}
