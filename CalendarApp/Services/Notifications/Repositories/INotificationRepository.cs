using CalendarApp.Services.Notifications.Models;

namespace CalendarApp.Services.Notifications.Repositories;

public interface INotificationRepository
{
    Task<IReadOnlyList<NotificationDto>> GetRecentAsync(Guid userId, int count, bool includeRead);
    Task<IReadOnlyList<NotificationDto>> GetAsync(Guid userId, NotificationQuery query);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId);
    Task<int> MarkAllAsReadAsync(Guid userId);
    Task<IReadOnlyCollection<NotificationRecord>> CreateAsync(IEnumerable<NotificationRecord> notifications);
}
