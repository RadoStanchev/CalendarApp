using CalendarApp.Models.Categories;
using CalendarApp.Services.Categories;
using CalendarApp.Services.Categories.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace CalendarApp.Controllers
{
    [Authorize]
    public class CategoriesController : Controller
    {
        private readonly ICategoryService categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            this.categoryService = categoryService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryCreateInputModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Моля, коригирайте грешките във формата.",
                    errors = ModelState
                        .Where(entry => entry.Value?.Errors.Count > 0)
                        .ToDictionary(
                            entry => entry.Key,
                            entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray())
                });
            }

            var dto = new CategoryCreateDto
            {
                Name = (model.Name ?? string.Empty).Trim(),
                Color = string.IsNullOrWhiteSpace(model.Color) ? null : model.Color!.Trim()
            };

            var categoryId = await categoryService.CreateAsync(dto);
            var createdCategory = await categoryService.GetByIdAsync(categoryId);

            return Json(new
            {
                id = createdCategory?.Id ?? categoryId,
                name = createdCategory?.Name ?? dto.Name,
                color = createdCategory?.Color ?? dto.Color
            });
        }
    }
}
