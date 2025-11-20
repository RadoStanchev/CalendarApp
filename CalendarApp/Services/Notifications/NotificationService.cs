using AutoMapper;
using AutoMapper.QueryableExtensions;
using CalendarApp.Data;
using CalendarApp.Data.Models;
using CalendarApp.Hubs;
using CalendarApp.Infrastructure.Time;
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

        public async Task<int> MarkAllAsReadAsync(Guid userId)
        {
            var notifications = await db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (notifications.Count == 0)
            {
                return 0;
            }

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await db.SaveChangesAsync();

            return notifications.Count;
        }

        public async Task SendMeetingReminderAsync(Meeting meeting, IEnumerable<Guid> recipientIds)
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

            var message = BuildReminderMessage(meeting);

            var notifications = recipients
                .Select(userId => new Notification
                {
                    UserId = userId,
                    Message = message,
                    Type = NotificationType.Reminder,
                    CreatedAt = DateTime.Now
                })
                .ToList();

            try
            {
                await db.Notifications.AddRangeAsync(notifications);
                await db.SaveChangesAsync();

                foreach (var notification in notifications)
                {
                    var payload = mapper.Map<MeetingReminderNotificationPayload>((notification, meeting));

                    await hubContext.Clients.User(notification.UserId.ToString()).SendAsync(
                        "ReceiveMeetingReminder",
                        payload);
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
            var startTimeLocal = BulgarianTime.ConvertUtcToLocal(meeting.StartTime);
            var description = string.IsNullOrWhiteSpace(meeting.Description)
                ? "Предстояща среща"
                : meeting.Description;

            return $"Напомняне: {description} започва на {startTimeLocal:dddd, MMM d yyyy HH:mm}.";
        }

        public async Task<NotificationDto> CreateNotificationAsync(NotificationCreateDto notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            var notifications = await CreateNotificationsAsync([notification]);
            return notifications.First();
        }

        public async Task<IReadOnlyCollection<NotificationDto>> CreateNotificationsAsync(IEnumerable<NotificationCreateDto> notifications)
        {
            if (notifications == null)
            {
                throw new ArgumentNullException(nameof(notifications));
            }

            var materialized = notifications
                .Where(n => n != null)
                .Select(mapper.Map<Notification>)
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

            await db.Notifications.AddRangeAsync(materialized);
            await db.SaveChangesAsync();

            var results = materialized
                .Select(mapper.Map<NotificationDto>)
                .ToList();

            var broadcastTasks = results
                .Select(result => hubContext.Clients
                    .User(result.UserId.ToString())
                    .SendAsync("ReceiveNotification", result))
                .ToList();

            if (broadcastTasks.Count > 0)
            {
                await Task.WhenAll(broadcastTasks);
            }

            return results;
        }
    }
}
