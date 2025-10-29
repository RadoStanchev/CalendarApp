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
