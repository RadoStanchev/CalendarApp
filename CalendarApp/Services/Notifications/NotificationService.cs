using CalendarApp.Data;
using CalendarApp.Data.Models;
using CalendarApp.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;
using CalendarApp.Data;
using CalendarApp.Data.Models;
using CalendarApp.Hubs;
using CalendarApp.Services.Notifications.Models;
using Microsoft.AspNetCore.SignalR;

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
        private readonly IMapper mapper;
        private readonly IHubContext<NotificationHub> hubContext;

        public NotificationService(ApplicationDbContext db, IMapper mapper, IHubContext<NotificationHub> hubContext)
        {
            this.db = db;
            this.mapper = mapper;
            this.hubContext = hubContext;
        }

        public async Task<NotificationDto> CreateNotificationAsync(NotificationCreateDto notification, CancellationToken cancellationToken = default)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            var notifications = await CreateNotificationsInternalAsync(new[] { notification }, cancellationToken);
            return notifications.First();
        }

        public async Task<IReadOnlyCollection<NotificationDto>> CreateNotificationsAsync(IEnumerable<NotificationCreateDto> notifications, CancellationToken cancellationToken = default)
        {
            return await CreateNotificationsInternalAsync(notifications, cancellationToken);
        }

        private async Task<IReadOnlyCollection<NotificationDto>> CreateNotificationsInternalAsync(IEnumerable<NotificationCreateDto> notifications, CancellationToken cancellationToken)
        {
            if (notifications == null)
            {
                throw new ArgumentNullException(nameof(notifications));
            }

            var materialized = notifications
                .Where(n => n != null)
                .Select(n => mapper.Map<Notification>(n))
                .ToList();

            if (materialized.Count == 0)
            {
                return Array.Empty<NotificationDto>();
            }

            foreach (var notification in materialized)
            {
                notification.CreatedAt = DateTime.UtcNow;
                notification.IsRead = false;
            }

            await db.Notifications.AddRangeAsync(materialized, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            var results = materialized
                .Select(mapper.Map<NotificationDto>)
                .ToList();

            var broadcastTasks = results
                .Select(result => hubContext.Clients.User(result.UserId.ToString())
                    .SendAsync("ReceiveNotification", result, cancellationToken))
                .ToList();

            if (broadcastTasks.Count > 0)
            {
                await Task.WhenAll(broadcastTasks);
            }

            return results;
        }
    }
}
