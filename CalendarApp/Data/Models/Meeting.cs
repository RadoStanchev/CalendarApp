using System.ComponentModel.DataAnnotations;

namespace CalendarApp.Data.Models
{
    public class Meeting
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public DateTime StartTime { get; set; }

        [StringLength(100)]
        public string? Location { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public Guid? CategoryId { get; set; }
        public Category? Category { get; set; }

        [Required]
        public Guid CreatedById { get; set; }
        public Contact CreatedBy { get; set; }

        public ICollection<MeetingParticipant> Participants { get; set; } = [];
    }
}
