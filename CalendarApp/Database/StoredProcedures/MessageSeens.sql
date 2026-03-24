CREATE OR ALTER PROCEDURE dbo.usp_MessageSeen_GetUnseenFriendshipMessageIds
    @UserId UNIQUEIDENTIFIER,
    @FriendshipId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT m.Id
    FROM dbo.Messages m
    WHERE m.FriendshipId = @FriendshipId
      AND m.SenderId <> @UserId
      AND NOT EXISTS (
          SELECT 1 FROM dbo.MessageSeens ms
          WHERE ms.MessageId = m.Id AND ms.ContactId = @UserId
      );
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_MessageSeen_GetUnseenMeetingMessageIds
    @UserId UNIQUEIDENTIFIER,
    @MeetingId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT m.Id
    FROM dbo.Messages m
    WHERE m.MeetingId = @MeetingId
      AND m.SenderId <> @UserId
      AND NOT EXISTS (
          SELECT 1 FROM dbo.MessageSeens ms
          WHERE ms.MessageId = m.Id AND ms.ContactId = @UserId
      );
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_MessageSeen_Insert
    @MessageId UNIQUEIDENTIFIER,
    @ContactId UNIQUEIDENTIFIER,
    @SeenAt DATETIME2
AS
BEGIN
    INSERT INTO dbo.MessageSeens (MessageId, ContactId, SeenAt)
    VALUES (@MessageId, @ContactId, @SeenAt);
END
GO
