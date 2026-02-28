namespace CalendarApp.Services.Notifications.Models
{
    using System;
    
    public class NotificationDto
    {
        public Guid Id { get; set; }

        public string Message { get; set; } = string.Empty;

        public Guid UserId { get; set; }

        public NotificationType Type { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
