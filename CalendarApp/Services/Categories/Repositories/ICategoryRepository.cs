using CalendarApp.Services.Categories.Models;

namespace CalendarApp.Services.Categories.Repositories;

public interface ICategoryRepository
{
    Task<IReadOnlyCollection<CategoryDetailsDto>> GetAllAsync();
    Task<CategoryDetailsDto?> GetByIdAsync(Guid categoryId);
    Task<Guid> CreateAsync(string name, string color);
    Task<bool> UpdateAsync(Guid id, string name, string color);
    Task<int> CountAsync();
    Task<bool> DeleteAsync(Guid categoryId);
    Task<bool> IsInUseAsync(Guid categoryId);
}
