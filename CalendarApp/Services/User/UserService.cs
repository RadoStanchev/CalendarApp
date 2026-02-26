using CalendarApp.Data.Models;
using CalendarApp.Services.User.Models;
using CalendarApp.Services.User.Repositories;

namespace CalendarApp.Services.User
{
    public class UserService : IUserService
    {
        private readonly IUserRepository userRepository;

        public UserService(IUserRepository userRepository)
        {
            this.userRepository = userRepository;
        }

        public Task<Contact?> GetByIdAsync(Guid id) => userRepository.GetByIdAsync(id);

        public Task<Contact?> GetByEmailAsync(string email) => userRepository.GetByEmailAsync(email);

        public Task<IEnumerable<Contact>> SearchAsync(string term) => userRepository.SearchAsync(term ?? string.Empty);

        public Task<IEnumerable<Contact>> GetAllAsync() => userRepository.GetAllAsync();

        public async Task<bool> UpdateProfileAsync(UpdateProfileDto dto)
        {
            var user = await userRepository.GetByIdAsync(dto.Id);
            if (user == null) return false;

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.BirthDate = dto.BirthDate;
            user.Address = dto.Address;
            user.Note = dto.Note;

            return await userRepository.UpdateProfileAsync(user);
        }

        public Task<bool> DeleteAsync(Guid id) => userRepository.DeleteAsync(id);
    }
}
