namespace CalendarApp.Models.Notifications
{
    using System;
    using System.Collections.Generic;
    using CalendarApp.Services.Notifications.Models;

    public class NotificationListViewModel
    {
        public NotificationReadFilter Filter { get; set; } = NotificationReadFilter.All;

        public int UnreadCount { get; set; }

        public IReadOnlyList<NotificationListItemViewModel> Notifications { get; set; } = Array.Empty<NotificationListItemViewModel>();
    }
}
