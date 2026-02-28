using CalendarApp.Infrastructure.Data;
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
        var items = await connection.QueryAsync<NotificationDto>(@"SELECT TOP (@count) Id, UserId, Message, Type, IsRead, CreatedAt
FROM dbo.Notifications
WHERE UserId = @userId AND (@includeRead = 1 OR IsRead = 0)
ORDER BY CreatedAt DESC", new { userId, count, includeRead });
        return items.ToList();
    }

    public async Task<IReadOnlyList<NotificationDto>> GetAsync(Guid userId, NotificationQuery query)
    {
        using var connection = connectionFactory.CreateConnection();
        var items = await connection.QueryAsync<NotificationDto>(@"SELECT TOP (CASE WHEN @limit IS NULL THEN 2147483647 ELSE @limit END)
Id, UserId, Message, Type, IsRead, CreatedAt
FROM dbo.Notifications
WHERE UserId = @userId
  AND (@filter = 0 OR (@filter = 1 AND IsRead = 0) OR (@filter = 2 AND IsRead = 1))
ORDER BY CreatedAt DESC", new { userId, filter = (int)query.Filter, limit = query.Limit });
        return items.ToList();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM dbo.Notifications WHERE UserId = @userId AND IsRead = 0", new { userId });
    }

    public async Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId)
    {
        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"UPDATE dbo.Notifications
SET IsRead = 1
WHERE UserId = @userId AND Id = @notificationId AND IsRead = 0", new { userId, notificationId });
        if (affected > 0)
        {
            return true;
        }

        return await connection.ExecuteScalarAsync<bool>("SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.Notifications WHERE UserId = @userId AND Id = @notificationId) THEN 1 ELSE 0 END AS bit)", new { userId, notificationId });
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(@"UPDATE dbo.Notifications SET IsRead = 1 WHERE UserId = @userId AND IsRead = 0", new { userId });
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
            await connection.ExecuteAsync(@"INSERT INTO dbo.Notifications (Id, UserId, Message, Type, IsRead, CreatedAt)
VALUES (@Id, @UserId, @Message, @Type, @IsRead, @CreatedAt)", notification, tx);
        }

        tx.Commit();
        return materialized;
    }

}
