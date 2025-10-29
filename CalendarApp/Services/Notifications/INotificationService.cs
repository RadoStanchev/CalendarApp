using CalendarApp.Data.Models;

namespace CalendarApp.Services.Notifications
{
    public interface INotificationService
    {
        Task SendMeetingReminderAsync(Meeting meeting, IEnumerable<Guid> recipientIds, CancellationToken cancellationToken = default);
    }
}
