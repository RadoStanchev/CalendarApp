using CalendarApp.Data.Models;
using System.Security.Claims;

namespace CalendarApp.Services.Auth;

public interface IAuthenticationService
{
    Task<(bool Succeeded, IEnumerable<string> Errors)> RegisterAsync(Contact user, string password, bool isPersistent);
    Task<bool> LoginAsync(string email, string password, bool rememberMe);
    Task LogoutAsync();
    Guid GetCurrentUserId(ClaimsPrincipal principal);
}
