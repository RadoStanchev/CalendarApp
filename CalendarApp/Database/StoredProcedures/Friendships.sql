CREATE OR ALTER PROCEDURE dbo.usp_Friendship_GetChatThreads
    @UserId UNIQUEIDENTIFIER,
    @AcceptedStatus INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.Id AS FriendshipId,
           CASE WHEN f.RequesterId = @UserId THEN f.ReceiverId ELSE f.RequesterId END AS FriendId,
           CASE WHEN f.RequesterId = @UserId THEN receiver.FirstName ELSE requester.FirstName END AS FriendFirstName,
           CASE WHEN f.RequesterId = @UserId THEN receiver.LastName ELSE requester.LastName END AS FriendLastName,
           CASE WHEN f.RequesterId = @UserId THEN receiver.Email ELSE requester.Email END AS FriendEmail,
           f.CreatedAt,
           lastMessage.Content AS LastMessageContent,
           lastMessage.SentAt AS LastMessageSentAt
    FROM dbo.Friendships f
    JOIN dbo.Contacts requester ON requester.Id = f.RequesterId
    JOIN dbo.Contacts receiver ON receiver.Id = f.ReceiverId
    OUTER APPLY (
        SELECT TOP (1) m.Content, m.SentAt
        FROM dbo.Messages m
        WHERE m.FriendshipId = f.Id
        ORDER BY m.SentAt DESC
    ) lastMessage
    WHERE f.Status = @AcceptedStatus
      AND (f.RequesterId = @UserId OR f.ReceiverId = @UserId)
    ORDER BY ISNULL(lastMessage.SentAt, f.CreatedAt) DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Friendship_GetChatThread
    @FriendshipId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @AcceptedStatus INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.Id AS FriendshipId,
           CASE WHEN f.RequesterId = @UserId THEN f.ReceiverId ELSE f.RequesterId END AS FriendId,
           CASE WHEN f.RequesterId = @UserId THEN receiver.FirstName ELSE requester.FirstName END AS FriendFirstName,
           CASE WHEN f.RequesterId = @UserId THEN receiver.LastName ELSE requester.LastName END AS FriendLastName,
           CASE WHEN f.RequesterId = @UserId THEN receiver.Email ELSE requester.Email END AS FriendEmail,
           f.CreatedAt
    FROM dbo.Friendships f
    JOIN dbo.Contacts requester ON requester.Id = f.RequesterId
    JOIN dbo.Contacts receiver ON receiver.Id = f.ReceiverId
    WHERE f.Id = @FriendshipId
      AND f.Status = @AcceptedStatus
      AND (f.RequesterId = @UserId OR f.ReceiverId = @UserId);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Friendship_GetFriends
    @UserId UNIQUEIDENTIFIER,
    @AcceptedStatus INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.Id AS FriendshipId,
           CASE WHEN f.RequesterId = @UserId THEN f.ReceiverId ELSE f.RequesterId END AS UserId,
           CASE WHEN f.RequesterId = @UserId THEN receiver.FirstName ELSE requester.FirstName END AS FirstName,
           CASE WHEN f.RequesterId = @UserId THEN receiver.LastName ELSE requester.LastName END AS LastName,
           CASE WHEN f.RequesterId = @UserId THEN receiver.Email ELSE requester.Email END AS Email
    FROM dbo.Friendships f
    JOIN dbo.Contacts requester ON requester.Id = f.RequesterId
    JOIN dbo.Contacts receiver ON receiver.Id = f.ReceiverId
    WHERE f.Status = @AcceptedStatus
      AND (f.RequesterId = @UserId OR f.ReceiverId = @UserId)
    ORDER BY FirstName, LastName;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Friendship_GetPendingRequests
    @UserId UNIQUEIDENTIFIER,
    @PendingStatus INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.Id AS FriendshipId,
           f.RequesterId,
           f.ReceiverId,
           CASE WHEN f.RequesterId = @UserId THEN f.ReceiverId ELSE f.RequesterId END AS TargetUserId,
           CASE WHEN f.RequesterId = @UserId THEN receiver.FirstName ELSE requester.FirstName END AS TargetFirstName,
           CASE WHEN f.RequesterId = @UserId THEN receiver.LastName ELSE requester.LastName END AS TargetLastName,
           CASE WHEN f.RequesterId = @UserId THEN receiver.Email ELSE requester.Email END AS TargetEmail,
           f.CreatedAt,
           CASE WHEN f.ReceiverId = @UserId THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsIncoming
    FROM dbo.Friendships f
    JOIN dbo.Contacts requester ON requester.Id = f.RequesterId
    JOIN dbo.Contacts receiver ON receiver.Id = f.ReceiverId
    WHERE f.Status = @PendingStatus
      AND (f.RequesterId = @UserId OR f.ReceiverId = @UserId)
    ORDER BY f.CreatedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Friendship_GetSuggestions
    @UserId UNIQUEIDENTIFIER,
    @MaxSuggestions INT,
    @PendingStatus INT,
    @AcceptedStatus INT,
    @BlockedStatus INT
AS
BEGIN
    SET NOCOUNT ON;
    WITH MyAcceptedFriends AS (
        SELECT CASE WHEN RequesterId = @UserId THEN ReceiverId ELSE RequesterId END AS FriendId
        FROM dbo.Friendships
        WHERE Status = @AcceptedStatus AND (RequesterId = @UserId OR ReceiverId = @UserId)
    )
    SELECT TOP (@MaxSuggestions)
        c.Id AS UserId,
        c.FirstName,
        c.LastName,
        c.Email,
        COUNT(DISTINCT fof.FriendId) AS MutualFriendCount
    FROM dbo.Contacts c
    LEFT JOIN (
        SELECT CASE WHEN f2.RequesterId = mf.FriendId THEN f2.ReceiverId ELSE f2.RequesterId END AS FriendId
        FROM dbo.Friendships f2
        JOIN MyAcceptedFriends mf ON (f2.RequesterId = mf.FriendId OR f2.ReceiverId = mf.FriendId)
        WHERE f2.Status = @AcceptedStatus
    ) fof ON fof.FriendId = c.Id
    WHERE c.Id <> @UserId
      AND NOT EXISTS (
          SELECT 1 FROM dbo.Friendships f
          WHERE ((f.RequesterId = @UserId AND f.ReceiverId = c.Id)
             OR (f.RequesterId = c.Id AND f.ReceiverId = @UserId))
            AND f.Status IN (@PendingStatus, @AcceptedStatus, @BlockedStatus)
      )
    GROUP BY c.Id, c.FirstName, c.LastName, c.Email
    ORDER BY MutualFriendCount DESC, c.FirstName, c.LastName;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Friendship_Search
    @UserId UNIQUEIDENTIFIER,
    @SearchTerm NVARCHAR(256),
    @ExcludedIds NVARCHAR(MAX) = NULL,
    @PendingStatus INT,
    @AcceptedStatus INT,
    @BlockedStatus INT,
    @NoneStatus INT,
    @FriendStatus INT,
    @IncomingStatus INT,
    @OutgoingStatus INT,
    @BlockedState INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Normalized NVARCHAR(260) = '%' + @SearchTerm + '%';

    SELECT c.Id AS UserId,
           c.FirstName,
           c.LastName,
           c.Email,
           CASE
             WHEN f.Status = @AcceptedStatus THEN @FriendStatus
             WHEN f.Status = @PendingStatus AND f.ReceiverId = @UserId THEN @IncomingStatus
             WHEN f.Status = @PendingStatus AND f.RequesterId = @UserId THEN @OutgoingStatus
             WHEN f.Status = @BlockedStatus THEN @BlockedState
             ELSE @NoneStatus
           END AS RelationshipStatus,
           f.Id AS FriendshipId,
           CASE WHEN f.Status = @PendingStatus AND f.ReceiverId = @UserId THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsIncomingRequest
    FROM dbo.Contacts c
    OUTER APPLY (
        SELECT TOP (1) Id, RequesterId, ReceiverId, Status
        FROM dbo.Friendships
        WHERE (RequesterId = @UserId AND ReceiverId = c.Id)
           OR (RequesterId = c.Id AND ReceiverId = @UserId)
        ORDER BY CreatedAt DESC
    ) f
    WHERE c.Id <> @UserId
      AND (
            @ExcludedIds IS NULL OR NOT EXISTS (
                SELECT 1
                FROM STRING_SPLIT(@ExcludedIds, ',') excluded
                WHERE TRY_CONVERT(uniqueidentifier, excluded.value) = c.Id
            )
      )
      AND (LOWER(c.FirstName) LIKE @Normalized OR LOWER(c.LastName) LIKE @Normalized OR LOWER(c.Email) LIKE @Normalized)
    ORDER BY c.FirstName, c.LastName;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Friendship_SendRequest
    @RequesterId UNIQUEIDENTIFIER,
    @ReceiverId UNIQUEIDENTIFIER,
    @PendingStatus INT,
    @AcceptedStatus INT,
    @BlockedStatus INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ExistingId UNIQUEIDENTIFIER;
    DECLARE @ExistingStatus INT;

    SELECT TOP (1) @ExistingId = Id, @ExistingStatus = Status
    FROM dbo.Friendships
    WHERE (RequesterId = @RequesterId AND ReceiverId = @ReceiverId)
       OR (RequesterId = @ReceiverId AND ReceiverId = @RequesterId)
    ORDER BY CreatedAt DESC;

    IF @ExistingId IS NOT NULL AND @ExistingStatus IN (@AcceptedStatus, @PendingStatus, @BlockedStatus)
    BEGIN
        SELECT CAST(0 AS bit) AS Success, @ExistingId AS FriendshipId;
        RETURN;
    END

    IF @ExistingId IS NOT NULL
    BEGIN
        UPDATE dbo.Friendships
        SET RequesterId = @RequesterId,
            ReceiverId = @ReceiverId,
            Status = @PendingStatus,
            CreatedAt = SYSUTCDATETIME()
        WHERE Id = @ExistingId;

        SELECT CAST(1 AS bit) AS Success, @ExistingId AS FriendshipId;
        RETURN;
    END

    DECLARE @Id UNIQUEIDENTIFIER = NEWID();
    INSERT INTO dbo.Friendships (Id, RequesterId, ReceiverId, Status, CreatedAt)
    VALUES (@Id, @RequesterId, @ReceiverId, @PendingStatus, SYSUTCDATETIME());

    SELECT CAST(1 AS bit) AS Success, @Id AS FriendshipId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Friendship_AcceptRequest
    @FriendshipId UNIQUEIDENTIFIER,
    @ReceiverId UNIQUEIDENTIFIER,
    @AcceptedStatus INT,
    @PendingStatus INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Friendships
    SET Status = @AcceptedStatus,
        CreatedAt = SYSUTCDATETIME()
    WHERE Id = @FriendshipId
      AND Status = @PendingStatus
      AND ReceiverId = @ReceiverId;

    IF @@ROWCOUNT = 0
    BEGIN
        SELECT CAST(0 AS bit) AS Success, CAST(NULL AS UNIQUEIDENTIFIER) AS RequesterId;
        RETURN;
    END

    SELECT CAST(1 AS bit) AS Success, RequesterId
    FROM dbo.Friendships
    WHERE Id = @FriendshipId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Friendship_DeclineRequest
    @FriendshipId UNIQUEIDENTIFIER,
    @ReceiverId UNIQUEIDENTIFIER,
    @DeclinedStatus INT,
    @PendingStatus INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Friendships
    SET Status = @DeclinedStatus,
        CreatedAt = SYSUTCDATETIME()
    WHERE Id = @FriendshipId
      AND Status = @PendingStatus
      AND ReceiverId = @ReceiverId;

    IF @@ROWCOUNT = 0
    BEGIN
        SELECT CAST(0 AS bit) AS Success, CAST(NULL AS UNIQUEIDENTIFIER) AS RequesterId;
        RETURN;
    END

    SELECT CAST(1 AS bit) AS Success, RequesterId
    FROM dbo.Friendships
    WHERE Id = @FriendshipId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Friendship_CancelRequest
    @FriendshipId UNIQUEIDENTIFIER,
    @RequesterId UNIQUEIDENTIFIER,
    @PendingStatus INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ReceiverId UNIQUEIDENTIFIER;

    SELECT @ReceiverId = ReceiverId
    FROM dbo.Friendships
    WHERE Id = @FriendshipId
      AND Status = @PendingStatus
      AND RequesterId = @RequesterId;

    IF @ReceiverId IS NULL
    BEGIN
        SELECT CAST(0 AS bit) AS Success, CAST(NULL AS UNIQUEIDENTIFIER) AS ReceiverId;
        RETURN;
    END

    DELETE FROM dbo.Friendships WHERE Id = @FriendshipId;
    SELECT CAST(1 AS bit) AS Success, @ReceiverId AS ReceiverId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Friendship_Remove
    @FriendshipId UNIQUEIDENTIFIER,
    @CancelerId UNIQUEIDENTIFIER,
    @AcceptedStatus INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.Friendships
    WHERE Id = @FriendshipId
      AND Status = @AcceptedStatus
      AND (RequesterId = @CancelerId OR ReceiverId = @CancelerId);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Friendship_GetContactFullName
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CONCAT(FirstName, ' ', LastName)
    FROM dbo.Contacts
    WHERE Id = @UserId;
END
GO
