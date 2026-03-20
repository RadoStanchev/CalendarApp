using CalendarApp.Services.User.Models;

namespace CalendarApp.Services.User
{
    public interface IUserService
    {
        Task<UserRecord?> GetByIdAsync(Guid id);
        Task<string?> GetFullNameAsync(Guid id);
        Task<UserRecord?> GetByEmailAsync(string email);
        Task<IEnumerable<UserRecord>> SearchAsync(string term);
        Task<IEnumerable<UserRecord>> GetAllAsync();
        Task<bool> UpdateProfileAsync(UpdateProfileDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
