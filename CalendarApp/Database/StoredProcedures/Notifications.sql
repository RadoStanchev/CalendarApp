CREATE OR ALTER PROCEDURE dbo.usp_Notification_GetRecent
    @UserId UNIQUEIDENTIFIER,
    @Count INT,
    @IncludeRead BIT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@Count) Id, UserId, Message, Type, IsRead, CreatedAt
    FROM dbo.Notifications
    WHERE UserId = @UserId AND (@IncludeRead = 1 OR IsRead = 0)
    ORDER BY CreatedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Notification_Get
    @UserId UNIQUEIDENTIFIER,
    @Filter INT,
    @Limit INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP @Limit
        Id, UserId, Message, Type, IsRead, CreatedAt
    FROM dbo.Notifications
    WHERE UserId = @UserId
      AND (@Filter = 0 OR (@Filter = 1 AND IsRead = 0) OR (@Filter = 2 AND IsRead = 1))
    ORDER BY CreatedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Notification_GetUnreadCount
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM dbo.Notifications WHERE UserId = @UserId AND IsRead = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Notification_MarkAsRead
    @UserId UNIQUEIDENTIFIER,
    @NotificationId UNIQUEIDENTIFIER
AS
BEGIN
    UPDATE dbo.Notifications
    SET IsRead = 1
    WHERE UserId = @UserId AND Id = @NotificationId AND IsRead = 0;

    IF @@ROWCOUNT > 0
    BEGIN
        SELECT CAST(1 AS bit);
        RETURN;
    END

    SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.Notifications WHERE UserId = @UserId AND Id = @NotificationId) THEN 1 ELSE 0 END AS bit);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Notification_MarkAllAsRead
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    UPDATE dbo.Notifications SET IsRead = 1 WHERE UserId = @UserId AND IsRead = 0;
    SELECT @@ROWCOUNT;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Notification_Create
    @Id UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @Message NVARCHAR(MAX),
    @Type INT,
    @IsRead BIT,
    @CreatedAt DATETIME2
AS
BEGIN
    INSERT INTO dbo.Notifications (Id, UserId, Message, Type, IsRead, CreatedAt)
    VALUES (@Id, @UserId, @Message, @Type, @IsRead, @CreatedAt);
END
GO
