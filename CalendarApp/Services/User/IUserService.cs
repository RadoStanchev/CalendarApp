using CalendarApp.Data.Models;
using CalendarApp.Services.User.Models;
using System;

namespace CalendarApp.Services.User
{
    public interface IUserService
    {
        Task<Contact?> GetByIdAsync(Guid id);
        Task<Contact?> GetByEmailAsync(string email);
        Task<IEnumerable<Contact>> SearchAsync(string term);
        Task<IEnumerable<Contact>> GetAllAsync();
        Task<bool> UpdateProfileAsync(UpdateProfileDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
