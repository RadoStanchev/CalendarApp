using System;
using System.Security.Claims;
using System.Threading.Tasks;
using CalendarApp.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace CalendarApp.Infrastructure.Extentions
{
    public static class UserManagerExtensions
    {
        public static async Task<Guid> GetUserIdGuidAsync<TUser>(this UserManager<TUser> userManager, ClaimsPrincipal principal)
            where TUser : Contact
        {
            var user = await userManager.GetUserAsync(principal);
            return user.Id;
        }
    }
}
