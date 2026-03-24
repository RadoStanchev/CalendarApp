using CalendarApp.Services.User.Models;
using CalendarApp.Repositories.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace CalendarApp.Services.Auth;

public class CookieAuthenticationService : IAuthenticationService
{
    private readonly IUserRepository userRepository;
    private readonly IPasswordHasher<UserRecord> passwordHasher;
    private readonly IHttpContextAccessor httpContextAccessor;

    public CookieAuthenticationService(
        IUserRepository userRepository,
        IPasswordHasher<UserRecord> passwordHasher,
        IHttpContextAccessor httpContextAccessor)
    {
        this.userRepository = userRepository;
        this.passwordHasher = passwordHasher;
        this.httpContextAccessor = httpContextAccessor;
    }

    public Guid GetCurrentUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Потребителят не е намерен.");

        return Guid.Parse(value);
    }

    public async Task<bool> LoginAsync(string email, string password, bool rememberMe)
    {
        var user = await userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            return false;
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash ?? string.Empty, password);
        if (result == PasswordVerificationResult.Failed)
        {
            return false;
        }

        await SignInAsync(user, rememberMe);
        return true;
    }

    public async Task LogoutAsync()
    {
        var context = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HTTP context is unavailable.");
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> RegisterAsync(UserRecord user, string password, bool isPersistent)
    {
        var existing = await userRepository.GetByEmailAsync(user.Email!);
        if (existing != null)
        {
            return (false, ["Потребител с този имейл вече съществува."]);
        }

        user.Id = Guid.NewGuid();
        user.PasswordHash = passwordHasher.HashPassword(user, password);
        user.EmailConfirmed = true;

        var created = await userRepository.CreateAsync(user);
        if (!created)
        {
            return (false, ["Регистрацията не бе успешна."]);
        }

        await SignInAsync(user, isPersistent);
        return (true, []);
    }

    private async Task SignInAsync(UserRecord user, bool isPersistent)
    {
        var context = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HTTP context is unavailable.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim() switch { { Length: > 0 } fullName => fullName, _ => user.Email ?? user.Id.ToString() }),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                AllowRefresh = true
            });
    }
}
