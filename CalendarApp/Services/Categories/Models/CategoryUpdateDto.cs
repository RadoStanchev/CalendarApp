namespace CalendarApp.Services.Categories.Models
{
    public class CategoryUpdateDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Color { get; set; }
    }
}
