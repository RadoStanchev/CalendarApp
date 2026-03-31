using System.Data;
using CalendarApp.Infrastructure.Data;
using CalendarApp.Services.Notifications.Models;
using Dapper;

namespace CalendarApp.Repositories.Notifications;

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
        var items = await connection.QueryAsync<NotificationDto>("dbo.usp_Notification_GetRecent", new { UserId = userId, Count = count, IncludeRead = includeRead }, commandType: CommandType.StoredProcedure);
        return items.ToList();
    }

    public async Task<IReadOnlyList<NotificationDto>> GetAsync(Guid userId, NotificationQuery query)
    {
        using var connection = connectionFactory.CreateConnection();
        var items = await connection.QueryAsync<NotificationDto>(
            "dbo.usp_Notification_Get", 
            new 
            { 
                UserId = userId, 
                Filter = (int)query.Filter, 
                Limit = query.Limit ?? 10 
            }, 
            commandType: CommandType.StoredProcedure);
            
        return items.ToList();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>("dbo.usp_Notification_GetUnreadCount", new { UserId = userId }, commandType: CommandType.StoredProcedure);
    }

    public async Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>("dbo.usp_Notification_MarkAsRead", new { UserId = userId, NotificationId = notificationId }, commandType: CommandType.StoredProcedure);
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>("dbo.usp_Notification_MarkAllAsRead", new { UserId = userId }, commandType: CommandType.StoredProcedure);
    }

    public async Task<IReadOnlyCollection<NotificationRecord>> CreateAsync(IEnumerable<NotificationRecord> notifications)
    {
        var materialized = notifications.ToList();
        if (materialized.Count == 0)
        {
            return materialized;
        }

        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var tx = connection.BeginTransaction();

        foreach (var notification in materialized)
        {
            await connection.ExecuteAsync("dbo.usp_Notification_Create", notification, tx, commandType: CommandType.StoredProcedure);
        }

        tx.Commit();
        return materialized;
    }
}
