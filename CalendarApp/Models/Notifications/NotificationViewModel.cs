using CalendarApp.Data.Models;

namespace CalendarApp.Models.Notifications
{
    public class NotificationViewModel
    {
        public Guid Id { get; set; }

        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
