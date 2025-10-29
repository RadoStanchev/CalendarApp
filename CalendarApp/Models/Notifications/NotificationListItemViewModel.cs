namespace CalendarApp.Models.Notifications
{
    using System;
    using CalendarApp.Data.Models;

    public class NotificationListItemViewModel
    {
        public Guid Id { get; set; }

        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
