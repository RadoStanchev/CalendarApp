namespace CalendarApp.Services.Notifications
{
    using CalendarApp.Services.Notifications.Models;

    public interface INotificationService
    {
        Task<IReadOnlyList<NotificationDto>> GetRecentAsync(Guid userId, int count, bool includeRead = false);

        Task<IReadOnlyList<NotificationDto>> GetAsync(Guid userId, NotificationQuery query);

        Task<int> GetUnreadCountAsync(Guid userId);

        Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId);
    }
}
