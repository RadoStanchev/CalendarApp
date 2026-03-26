using CalendarApp.Services.User.Models;
using CalendarApp.Repositories.User;

namespace CalendarApp.Services.User
{
    public class UserService : IUserService
    {
        private readonly IUserRepository userRepository;

        public UserService(IUserRepository userRepository)
        {
            this.userRepository = userRepository;
        }

        public Task<UserRecord?> GetByIdAsync(Guid id) => userRepository.GetByIdAsync(id);

        public Task<string?> GetFullNameAsync(Guid id) => userRepository.GetFullNameAsync(id);

        public Task<UserRecord?> GetByEmailAsync(string email) => userRepository.GetByEmailAsync(email);

        public Task<IEnumerable<UserRecord>> SearchAsync(string term) => userRepository.SearchAsync(term ?? string.Empty);

        public Task<IEnumerable<UserRecord>> GetAllAsync() => userRepository.GetAllAsync();

        public Task<bool> UpdateProfileAsync(UpdateProfileDto dto) => userRepository.UpdateProfileAsync(dto);

        public Task<bool> DeleteAsync(Guid id) => userRepository.DeleteAsync(id);
    }
}
