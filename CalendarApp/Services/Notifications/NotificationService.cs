using CalendarApp.Data;
using CalendarApp.Data.Models;
using CalendarApp.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CalendarApp.Services.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext db;
        private readonly IHubContext<NotificationHub> hubContext;
        private readonly ILogger<NotificationService> logger;

        public NotificationService(ApplicationDbContext db, IHubContext<NotificationHub> hubContext, ILogger<NotificationService> logger)
        {
            this.db = db;
            this.hubContext = hubContext;
            this.logger = logger;
        }

        public async Task SendMeetingReminderAsync(Meeting meeting, IEnumerable<Guid> recipientIds, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(meeting);

            var recipients = recipientIds
                ?.Where(id => id != Guid.Empty)
                .Distinct()
                .ToList() ?? [];

            if (recipients.Count == 0)
            {
                return;
            }

            var notifications = new List<Notification>();
            foreach (var userId in recipients)
            {
                var message = BuildReminderMessage(meeting);
                notifications.Add(new Notification
                {
                    UserId = userId,
                    Message = message,
                    Type = NotificationType.Reminder,
                    CreatedAt = DateTime.Now
                });
            }

            try
            {
                await db.Notifications.AddRangeAsync(notifications, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);

                foreach (var notification in notifications)
                {
                    var payload = new
                    {
                        notificationId = notification.Id,
                        message = notification.Message,
                        meetingId = meeting.Id,
                        meetingStartTime = meeting.StartTime,
                        meetingLocation = meeting.Location,
                        meetingDescription = meeting.Description
                    };

                    await hubContext.Clients.User(notification.UserId.ToString()).SendAsync(
                        "ReceiveMeetingReminder",
                        payload,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send meeting reminder notifications for meeting {MeetingId}.", meeting.Id);
                throw;
            }
        }

        private static string BuildReminderMessage(Meeting meeting)
        {
            var description = string.IsNullOrWhiteSpace(meeting.Description)
                ? "Upcoming meeting"
                : meeting.Description;

            return $"Reminder: {description} starts at {meeting.StartTime:dddd, MMM d yyyy h:mm tt}.";
        }
    }
}
