using CalendarApp.Services.Categories.Models;

namespace CalendarApp.Services.Categories
{
    public interface ICategoryService
    {
        Task<IReadOnlyCollection<CategorySummaryDto>> GetAllAsync();

        Task<CategoryDetailsDto?> GetByIdAsync(Guid categoryId);

        Task<Guid> CreateAsync(CategoryCreateDto dto);

        Task<bool> UpdateAsync(CategoryUpdateDto dto);

        Task<CategoryDeletionResult> DeleteAsync(Guid categoryId);

        Task<bool> IsInUseAsync(Guid categoryId);

        Task<int> RemoveAssignmentsAsync(Guid categoryId);
    }
}
