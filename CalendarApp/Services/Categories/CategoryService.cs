using CalendarApp.Data;
using CalendarApp.Data.Models;
using CalendarApp.Services.Categories.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace CalendarApp.Services.Categories
{
    public class CategoryService : ICategoryService
    {
        private static readonly string[] DefaultColors = new[]
        {
            "#0D6EFD", // primary
            "#6610F2",
            "#6F42C1",
            "#D63384",
            "#DC3545",
            "#FD7E14",
            "#FFC107",
            "#198754",
            "#20C997",
            "#0DCAF0"
        };

        private readonly ApplicationDbContext db;

        public CategoryService(ApplicationDbContext db)
        {
            this.db = db;
        }

        public async Task<IReadOnlyCollection<CategoryDetailsDto>> GetAllAsync()
        {
            return await db.Categories
                .AsNoTracking()
                .Select(c => new CategoryDetailsDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Color = c.Color,
                    MeetingCount = c.Meetings.Count()
                })
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<CategoryDetailsDto?> GetByIdAsync(Guid categoryId)
        {
            return await db.Categories
                .AsNoTracking()
                .Where(c => c.Id == categoryId)
                .Select(c => new CategoryDetailsDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Color = c.Color,
                    MeetingCount = c.Meetings.Count()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<Guid> CreateAsync(CategoryCreateDto dto)
        {
            var name = (dto.Name ?? string.Empty).Trim();
            var color = string.IsNullOrWhiteSpace(dto.Color) ? null : dto.Color!.Trim();

            var category = new Category
            {
                Name = name,
                Color = color ?? await PickAutomaticColorAsync()
            };

            db.Categories.Add(category);
            await db.SaveChangesAsync();

            return category.Id;
        }

        public async Task<bool> UpdateAsync(CategoryUpdateDto dto)
        {
            var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == dto.Id);

            if (category == null)
            {
                return false;
            }

            category.Name = (dto.Name ?? string.Empty).Trim();

            var color = string.IsNullOrWhiteSpace(dto.Color) ? null : dto.Color!.Trim();
            category.Color = color ?? await PickAutomaticColorAsync();

            await db.SaveChangesAsync();
            return true;
        }

        private async Task<string> PickAutomaticColorAsync()
        {
            var totalCategories = await db.Categories.CountAsync();
            var index = totalCategories % DefaultColors.Length;
            return DefaultColors[index];
        }

        public async Task<bool> DeleteAsync(Guid categoryId)
        {
            var category = await db.Categories
                .Include(c => c.Meetings)
                .FirstOrDefaultAsync(c => c.Id == categoryId);

            if (category == null)
            {
                return false;
            }

            db.Categories.Remove(category);
            await db.SaveChangesAsync();

            return true;
        }

        public async Task<bool> IsInUseAsync(Guid categoryId)
        {
            return await db.Meetings.AnyAsync(m => m.CategoryId == categoryId);
        }
    }
}
