namespace CalendarApp.Infrastructure.Data.Sql;

public static class NotificationsSql
{
    public const string SelectRecent = @"SELECT TOP (@count) Id, UserId, Message, Type, IsRead, CreatedAt
FROM dbo.Notifications
WHERE UserId = @userId AND (@includeRead = 1 OR IsRead = 0)
ORDER BY CreatedAt DESC";

    public const string SelectByFilter = @"SELECT TOP (CASE WHEN @limit IS NULL THEN 2147483647 ELSE @limit END)
Id, UserId, Message, Type, IsRead, CreatedAt
FROM dbo.Notifications
WHERE UserId = @userId
  AND (@filter = 0 OR (@filter = 1 AND IsRead = 0) OR (@filter = 2 AND IsRead = 1))
ORDER BY CreatedAt DESC";

    public const string CountUnread = "SELECT COUNT(1) FROM dbo.Notifications WHERE UserId = @userId AND IsRead = 0";

    public const string MarkAsRead = @"UPDATE dbo.Notifications
SET IsRead = 1
WHERE UserId = @userId AND Id = @notificationId AND IsRead = 0";

    public const string Exists = "SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.Notifications WHERE UserId = @userId AND Id = @notificationId) THEN 1 ELSE 0 END AS bit)";

    public const string MarkAllAsRead = @"UPDATE dbo.Notifications SET IsRead = 1 WHERE UserId = @userId AND IsRead = 0";

    public const string Insert = @"INSERT INTO dbo.Notifications (Id, UserId, Message, Type, IsRead, CreatedAt)
VALUES (@Id, @UserId, @Message, @Type, @IsRead, @CreatedAt)";
}
