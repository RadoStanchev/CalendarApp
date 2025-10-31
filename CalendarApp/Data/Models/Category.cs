using System.ComponentModel.DataAnnotations;

namespace CalendarApp.Data.Models
{
    public class Category
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(50)]
        public string Name { get; set; }

        [StringLength(20)]
        public string? Color { get; set; }

        public ICollection<Meeting> Meetings { get; set; } = [];
    }
}
