namespace CalendarApp.Services.Notifications.Models
{
    public enum NotificationReadFilter
    {
        All,
        Unread,
        Read
    }

    public class NotificationQuery
    {
        public NotificationReadFilter Filter { get; set; } = NotificationReadFilter.All;

        public int? Limit { get; set; }
    }
}
