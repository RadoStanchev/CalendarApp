using CalendarApp.Services.User.Models;

namespace CalendarApp.Services.User.Repositories;

public interface IUserRepository
{
    Task<UserRecord?> GetByIdAsync(Guid id);
    Task<UserRecord?> GetByEmailAsync(string email);
    Task<IEnumerable<UserRecord>> SearchAsync(string term);
    Task<IEnumerable<UserRecord>> GetAllAsync();
    Task<bool> UpdateProfileAsync(UserRecord user);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> CreateAsync(UserRecord user);
}
