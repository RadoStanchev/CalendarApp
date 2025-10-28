using System.ComponentModel.DataAnnotations;

namespace CalendarApp.Data.Models
{
    public enum NotificationType
    {
        Info,
        Warning,
        Invitation,
        Reminder
    }

    public class Notification
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }
        public Contact User { get; set; }

        [Required, StringLength(200)]
        public string Message { get; set; }

        [Required]
        public NotificationType Type { get; set; } = NotificationType.Info;

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
