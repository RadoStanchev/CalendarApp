using AutoMapper;
using CalendarApp.Hubs;
using CalendarApp.Infrastructure.Time;
using CalendarApp.Services.Meetings.Models;
using CalendarApp.Services.Notifications.Models;
using CalendarApp.Repositories.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace CalendarApp.Services.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository notificationRepository;
        private readonly IHubContext<NotificationHub> hubContext;
        private readonly ILogger<NotificationService> logger;
        private readonly IMapper mapper;

        public NotificationService(INotificationRepository notificationRepository, IMapper mapper, ILogger<NotificationService> logger, IHubContext<NotificationHub> hubContext)
        {
            this.notificationRepository = notificationRepository;
            this.mapper = mapper;
            this.logger = logger;
            this.hubContext = hubContext;
        }

        public Task<IReadOnlyList<NotificationDto>> GetRecentAsync(Guid userId, int count, bool includeRead = false)
            => notificationRepository.GetRecentAsync(userId, count, includeRead);

        public Task<IReadOnlyList<NotificationDto>> GetAsync(Guid userId, NotificationQuery queryModel)
            => notificationRepository.GetAsync(userId, queryModel);

        public Task<int> GetUnreadCountAsync(Guid userId)
            => notificationRepository.GetUnreadCountAsync(userId);

        public Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId)
            => notificationRepository.MarkAsReadAsync(userId, notificationId);

        public Task<int> MarkAllAsReadAsync(Guid userId)
            => notificationRepository.MarkAllAsReadAsync(userId);

        public async Task SendMeetingReminderAsync(MeetingRecord meeting, IEnumerable<Guid> recipientIds)
        {
            ArgumentNullException.ThrowIfNull(meeting);

            var recipients = recipientIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? [];
            if (recipients.Count == 0) return;

            var message = BuildReminderMessage(meeting);
            var notifications = recipients.Select(userId => new NotificationRecord
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Message = message,
                Type = NotificationType.Reminder,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            }).ToList();

            try
            {
                await notificationRepository.CreateAsync(notifications);
                foreach (var notification in notifications)
                {
                    var payload = mapper.Map<MeetingReminderNotificationPayload>((notification, meeting));
                    await hubContext.Clients.User(notification.UserId.ToString()).SendAsync("ReceiveMeetingReminder", payload);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send meeting reminder notifications for meeting {MeetingId}.", meeting.Id);
                throw;
            }
        }

        private static string BuildReminderMessage(MeetingRecord meeting)
        {
            var startTimeLocal = BulgarianTime.ConvertUtcToLocal(meeting.StartTime);
            var description = string.IsNullOrWhiteSpace(meeting.Description) ? "Предстояща среща" : meeting.Description;
            return $"Напомняне: {description} започва на {startTimeLocal:dddd, MMM d yyyy HH:mm}.";
        }

        public async Task<NotificationDto> CreateNotificationAsync(NotificationCreateDto notification)
        {
            var notifications = await CreateNotificationsAsync([notification]);
            return notifications.First();
        }

        public async Task<IReadOnlyCollection<NotificationDto>> CreateNotificationsAsync(IEnumerable<NotificationCreateDto> notifications)
        {
            if (notifications == null) throw new ArgumentNullException(nameof(notifications));

            var materialized = notifications.Where(n => n != null).Select(mapper.Map<NotificationRecord>).ToList();
            if (materialized.Count == 0) return Array.Empty<NotificationDto>();

            foreach (var n in materialized)
            {
                n.Id = Guid.NewGuid();
                n.CreatedAt = DateTime.UtcNow;
                n.IsRead = false;
            }

            await notificationRepository.CreateAsync(materialized);
            var results = materialized.Select(mapper.Map<NotificationDto>).ToList();

            var broadcastTasks = results.Select(result => hubContext.Clients.User(result.UserId.ToString()).SendAsync("ReceiveNotification", result));
            await Task.WhenAll(broadcastTasks);

            return results;
        }
    }
}
