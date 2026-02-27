using CalendarApp.Services.Categories.Models;
using CalendarApp.Services.Categories.Repositories;

namespace CalendarApp.Services.Categories
{
    public class CategoryService : ICategoryService
    {
        private static readonly string[] DefaultColors =
        [
            "#0D6EFD",
            "#6610F2",
            "#6F42C1",
            "#D63384",
            "#DC3545",
            "#FD7E14",
            "#FFC107",
            "#198754",
            "#20C997",
            "#0DCAF0"
        ];

        private readonly ICategoryRepository categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            this.categoryRepository = categoryRepository;
        }

        public Task<IReadOnlyCollection<CategoryDetailsDto>> GetAllAsync() => categoryRepository.GetAllAsync();

        public Task<CategoryDetailsDto?> GetByIdAsync(Guid categoryId) => categoryRepository.GetByIdAsync(categoryId);

        public async Task<Guid> CreateAsync(CategoryCreateDto dto)
        {
            var name = (dto.Name ?? string.Empty).Trim();
            var color = string.IsNullOrWhiteSpace(dto.Color) ? await PickAutomaticColorAsync() : dto.Color!.Trim();
            return await categoryRepository.CreateAsync(name, color);
        }

        public async Task<bool> UpdateAsync(CategoryUpdateDto dto)
        {
            var color = string.IsNullOrWhiteSpace(dto.Color) ? await PickAutomaticColorAsync() : dto.Color!.Trim();
            return await categoryRepository.UpdateAsync(dto.Id, (dto.Name ?? string.Empty).Trim(), color);
        }

        private async Task<string> PickAutomaticColorAsync()
        {
            var totalCategories = await categoryRepository.CountAsync();
            return DefaultColors[totalCategories % DefaultColors.Length];
        }

        public Task<bool> DeleteAsync(Guid categoryId) => categoryRepository.DeleteAsync(categoryId);

        public Task<bool> IsInUseAsync(Guid categoryId) => categoryRepository.IsInUseAsync(categoryId);
    }
}
