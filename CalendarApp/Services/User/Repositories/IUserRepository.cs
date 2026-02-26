using CalendarApp.Data.Models;

namespace CalendarApp.Services.User.Repositories;

public interface IUserRepository
{
    Task<Contact?> GetByIdAsync(Guid id);
    Task<Contact?> GetByEmailAsync(string email);
    Task<IEnumerable<Contact>> SearchAsync(string term);
    Task<IEnumerable<Contact>> GetAllAsync();
    Task<bool> UpdateProfileAsync(Contact user);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> CreateAsync(Contact user);
}
