namespace CalendarApp.Services.Notifications
{
    using CalendarApp.Services.Notifications.Models;

    public interface INotificationService
    {
        Task<IReadOnlyList<NotificationDto>> GetRecentAsync(Guid userId, int count, bool includeRead = false);

        Task<IReadOnlyList<NotificationDto>> GetAsync(Guid userId, NotificationQuery query);

        Task<int> GetUnreadCountAsync(Guid userId);

        Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId);
using CalendarApp.Data.Models;
using CalendarApp.Services.Notifications.Models;

namespace CalendarApp.Services.Notifications
{
    public interface INotificationService
    {
        Task SendMeetingReminderAsync(Meeting meeting, IEnumerable<Guid> recipientIds, CancellationToken cancellationToken = default);
        Task<NotificationDto> CreateNotificationAsync(NotificationCreateDto notification, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<NotificationDto>> CreateNotificationsAsync(IEnumerable<NotificationCreateDto> notifications, CancellationToken cancellationToken = default);
    }
}
