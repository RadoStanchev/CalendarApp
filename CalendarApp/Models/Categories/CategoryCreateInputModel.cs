using System.ComponentModel.DataAnnotations;

namespace CalendarApp.Models.Categories
{
    public class CategoryCreateInputModel
    {
        [Required]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Името трябва да бъде между 2 и 50 символа.")]
        [Display(Name = "Име на категория")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Цвят (HEX)")]
        [RegularExpression(@"^(#(?:[0-9a-fA-F]{3}){1,2})?$", ErrorMessage = "Цветът трябва да е валиден HEX код, например #0D6EFD.")]
        public string? Color { get; set; }
    }
}
