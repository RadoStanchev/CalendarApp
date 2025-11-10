using CalendarApp.Data;
using CalendarApp.Data.Models;
using CalendarApp.Services.User.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CalendarApp.Services.User
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<Contact> userManager;

        public UserService(ApplicationDbContext db, UserManager<Contact> userManager)
        {
            this.db = db;
            this.userManager = userManager;
        }

        public async Task<Contact?> GetByIdAsync(Guid id)
        {
            return await db.Users
                .Include(u => u.SentFriendRequests)
                .Include(u => u.ReceivedFriendRequests)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<Contact?> GetByEmailAsync(string email)
        {
            return await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<IEnumerable<Contact>> SearchAsync(string term)
        {
            term = term?.ToLower() ?? string.Empty;
            return await db.Users
                .Where(u => u.FirstName.ToLower().Contains(term)
                         || u.LastName.ToLower().Contains(term)
                         || u.Email.ToLower().Contains(term))
                .OrderBy(u => u.FirstName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Contact>> GetAllAsync()
        {
            return await db.Users
                .OrderBy(u => u.FirstName)
                .ToListAsync();
        }

        public async Task<bool> UpdateProfileAsync(UpdateProfileDto dto)
        {
            var user = await userManager.FindByIdAsync(dto.Id.ToString());
            if (user == null) return false;

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.BirthDate = dto.BirthDate;
            user.Address = dto.Address;
            user.Note = dto.Note;

            var result = await userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var user = await userManager.FindByIdAsync(id.ToString());
            if (user == null) return false;

            var result = await userManager.DeleteAsync(user);
            return result.Succeeded;
        }
    }
}
