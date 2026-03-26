CREATE OR ALTER PROCEDURE dbo.usp_Message_HasFriendshipAccess
    @UserId UNIQUEIDENTIFIER,
    @FriendshipId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @AcceptedStatus INT = (SELECT Id FROM dbo.FriendshipStatuses WHERE Name = N'Accepted');
    SELECT CAST(CASE WHEN EXISTS (
        SELECT * FROM dbo.Friendships
        WHERE Id = @FriendshipId
          AND StatusId = @AcceptedStatus
          AND (RequesterId = @UserId OR ReceiverId = @UserId)
    ) THEN 1 ELSE 0 END AS bit);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Message_HasMeetingAccess
    @UserId UNIQUEIDENTIFIER,
    @MeetingId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @AcceptedStatus INT = (SELECT TOP 1 Id FROM dbo.ParticipantStatuses WHERE Name = N'Accepted');

    SELECT CAST(CASE WHEN EXISTS (
        SELECT 1 FROM dbo.Meetings m
        WHERE m.Id = @MeetingId
          AND (
              m.CreatedById = @UserId
              OR EXISTS (
                  SELECT 1 FROM dbo.MeetingParticipants mp
                  WHERE mp.MeetingId = m.Id AND mp.ContactId = @UserId AND mp.StatusId = @AcceptedStatus
              )
          )
    ) THEN 1 ELSE 0 END AS bit);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Message_GetSender
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 Id, FirstName, LastName FROM dbo.Users WHERE Id = @UserId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Message_GetMeetingInfo
    @MeetingId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 Id, StartTime, Location FROM dbo.Meetings WHERE Id = @MeetingId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Message_Insert
    @Id UNIQUEIDENTIFIER,
    @FriendshipId UNIQUEIDENTIFIER = NULL,
    @MeetingId UNIQUEIDENTIFIER = NULL,
    @SenderId UNIQUEIDENTIFIER,
    @Content NVARCHAR(MAX),
    @SentAt DATETIME2
AS
BEGIN
    INSERT INTO dbo.Messages (Id, FriendshipId, MeetingId, SenderId, Content, SentAt)
    VALUES (@Id, @FriendshipId, @MeetingId, @SenderId, @Content, @SentAt);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Message_GetRecentFriendshipMessages
    @FriendshipId UNIQUEIDENTIFIER,
    @Take INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@Take)
        m.FriendshipId,
        m.MeetingId,
        m.Id AS MessageId,
        m.SenderId,
        CONCAT(c.FirstName, ' ', c.LastName) AS SenderName,
        m.Content,
        m.SentAt
    FROM dbo.Messages m
    INNER JOIN dbo.Users c ON c.Id = m.SenderId
    WHERE m.FriendshipId = @FriendshipId
    ORDER BY m.SentAt DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Message_GetRecentMeetingMessages
    @MeetingId UNIQUEIDENTIFIER,
    @Take INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@Take)
        m.FriendshipId,
        m.MeetingId,
        m.Id AS MessageId,
        m.SenderId,
        CONCAT(c.FirstName, ' ', c.LastName) AS SenderName,
        m.Content,
        m.SentAt
    FROM dbo.Messages m
    INNER JOIN dbo.Users c ON c.Id = m.SenderId
    WHERE m.MeetingId = @MeetingId
    ORDER BY m.SentAt DESC;
END
GO
