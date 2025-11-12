namespace CalendarApp.Services.Notifications
{
    using CalendarApp.Data.Models;
    using CalendarApp.Services.Notifications.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface INotificationService
    {
        Task<IReadOnlyList<NotificationDto>> GetRecentAsync(Guid userId, int count, bool includeRead = false);

        Task<IReadOnlyList<NotificationDto>> GetAsync(Guid userId, NotificationQuery query);

        Task<int> GetUnreadCountAsync(Guid userId);

        Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId);

        Task<int> MarkAllAsReadAsync(Guid userId);

        Task SendMeetingReminderAsync(Meeting meeting, IEnumerable<Guid> recipientIds);
        Task<NotificationDto> CreateNotificationAsync(NotificationCreateDto notification);

        Task<IReadOnlyCollection<NotificationDto>> CreateNotificationsAsync(IEnumerable<NotificationCreateDto> notifications);
    }
}
