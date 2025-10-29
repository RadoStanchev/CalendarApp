using CalendarApp.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace CalendarApp.Services.Notifications.Models
{
    public class NotificationCreateDto
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; } = NotificationType.Info;
    }
}
