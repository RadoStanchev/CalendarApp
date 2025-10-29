using AutoMapper;
using AutoMapper.QueryableExtensions;
using CalendarApp.Data;
using CalendarApp.Data.Models;
using CalendarApp.Hubs;
using CalendarApp.Services.Notifications.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;

namespace CalendarApp.Services.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext db;
        private readonly IHubContext<NotificationHub> hubContext;
        private readonly ILogger<NotificationService> logger;
        private readonly IMapper mapper;

        public NotificationService(ApplicationDbContext db, IMapper mapper, ILogger<NotificationService> logger, IHubContext<NotificationHub> hubContext)
        {
            this.db = db;
            this.mapper = mapper;
            this.logger = logger;
            this.hubContext = hubContext;
        }

        public async Task<IReadOnlyList<NotificationDto>> GetRecentAsync(Guid userId, int count, bool includeRead = false)
        {
            var query = db.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId);

            if (!includeRead)
            {
                query = query.Where(n => !n.IsRead);
            }

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .ProjectTo<NotificationDto>(mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<NotificationDto>> GetAsync(Guid userId, NotificationQuery queryModel)
        {
            var query = db.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId);

            query = queryModel.Filter switch
            {
                NotificationReadFilter.Unread => query.Where(n => !n.IsRead),
                NotificationReadFilter.Read => query.Where(n => n.IsRead),
                _ => query
            };

            query = query.OrderByDescending(n => n.CreatedAt);

            if (queryModel.Limit.HasValue)
            {
                query = query.Take(queryModel.Limit.Value);
            }

            return await query
                .ProjectTo<NotificationDto>(mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await db.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
        }

        public async Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId)
        {
            var notification = await db.Notifications
                .Where(n => n.UserId == userId && n.Id == notificationId)
                .FirstOrDefaultAsync();

            if (notification == null)
            {
                return false;
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await db.SaveChangesAsync();
            }

            return true;
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
                .Select(result => hubContext.Clients.User(result.Id.ToString())
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
