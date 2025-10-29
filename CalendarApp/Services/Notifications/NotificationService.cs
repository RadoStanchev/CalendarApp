namespace CalendarApp.Services.Notifications
{
    using System;
    using AutoMapper;
    using AutoMapper.QueryableExtensions;
    using CalendarApp.Data;
    using CalendarApp.Services.Notifications.Models;
    using Microsoft.EntityFrameworkCore;

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext db;
        private readonly IMapper mapper;

        public NotificationService(ApplicationDbContext db, IMapper mapper)
        {
            this.db = db;
            this.mapper = mapper;
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
    }
}
