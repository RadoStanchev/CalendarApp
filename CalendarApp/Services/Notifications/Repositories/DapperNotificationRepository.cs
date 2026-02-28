using CalendarApp.Infrastructure.Data;
using CalendarApp.Infrastructure.Data.Sql;
using CalendarApp.Services.Notifications.Models;
using Dapper;

namespace CalendarApp.Services.Notifications.Repositories;

public class DapperNotificationRepository : INotificationRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public DapperNotificationRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetRecentAsync(Guid userId, int count, bool includeRead)
    {
        using var connection = connectionFactory.CreateConnection();
        var items = await connection.QueryAsync<NotificationDto>(NotificationsSql.SelectRecent, new { userId, count, includeRead });
        return items.ToList();
    }

    public async Task<IReadOnlyList<NotificationDto>> GetAsync(Guid userId, NotificationQuery query)
    {
        using var connection = connectionFactory.CreateConnection();
        var items = await connection.QueryAsync<NotificationDto>(NotificationsSql.SelectByFilter, new { userId, filter = (int)query.Filter, limit = query.Limit });
        return items.ToList();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(NotificationsSql.CountUnread, new { userId });
    }

    public async Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId)
    {
        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(NotificationsSql.MarkAsRead, new { userId, notificationId });
        if (affected > 0)
        {
            return true;
        }

        return await connection.ExecuteScalarAsync<bool>(NotificationsSql.Exists, new { userId, notificationId });
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(NotificationsSql.MarkAllAsRead, new { userId });
    }

    public async Task<IReadOnlyCollection<NotificationRecord>> CreateAsync(IEnumerable<NotificationRecord> notifications)
    {
        var materialized = notifications.ToList();
        if (materialized.Count == 0)
        {
            return materialized;
        }

        using var connection = connectionFactory.CreateConnection();
        using var tx = connection.BeginTransaction();

        foreach (var notification in materialized)
        {
            await connection.ExecuteAsync(NotificationsSql.Insert, notification, tx);
        }

        tx.Commit();
        return materialized;
    }
}
