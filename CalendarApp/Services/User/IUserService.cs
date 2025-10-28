using CalendarApp.Data.Models;

namespace CalendarApp.Services.User
{
    public interface IUserService
    {
        Task<Contact?> GetByIdAsync(Guid id);
        Task<Contact?> GetByEmailAsync(string email);
        Task<IEnumerable<Contact>> SearchAsync(string term);
        Task<IEnumerable<Contact>> GetAllAsync();
        Task<bool> UpdateProfileAsync(Guid id, string? firstName, string? lastName, string? address);
        Task<bool> DeleteAsync(Guid id);
    }
}