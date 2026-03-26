CREATE OR ALTER PROCEDURE dbo.usp_Meeting_GetById
    @MeetingId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) Id, StartTime, Location, Description, CategoryId, CreatedById, ReminderSent
    FROM dbo.Meetings
    WHERE Id = @MeetingId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Meeting_GetChatThreads
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @AcceptedParticipantStatusId INT = (SELECT TOP 1 Id FROM dbo.ParticipantStatuses WHERE Name = N'Accepted');
    SELECT m.Id AS MeetingId,
           m.Description,
           m.StartTime,
           m.Location,
           m.CreatedById,
           creator.FirstName AS CreatorFirstName,
           creator.LastName AS CreatorLastName,
           (SELECT COUNT(*) FROM dbo.MeetingParticipants mp WHERE mp.MeetingId = m.Id AND mp.StatusId = @AcceptedParticipantStatusId) AS ParticipantCount,
           lastMessage.Content AS LastMessageContent,
           lastMessage.SentAt AS LastMessageSentAt
    FROM dbo.Meetings m
    JOIN dbo.Users creator ON creator.Id = m.CreatedById
    OUTER APPLY (
        SELECT TOP (1) Content, SentAt
        FROM dbo.Messages msg
        WHERE msg.MeetingId = m.Id
        ORDER BY msg.SentAt DESC
    ) lastMessage
    WHERE m.CreatedById = @UserId
       OR EXISTS (SELECT 1 FROM dbo.MeetingParticipants mp WHERE mp.MeetingId = m.Id AND mp.ContactId = @UserId AND mp.StatusId = @AcceptedParticipantStatusId)
    ORDER BY ISNULL(lastMessage.SentAt, m.StartTime) DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Meeting_GetChatThread
    @MeetingId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @AcceptedParticipantStatusId INT = (SELECT TOP 1 Id FROM dbo.ParticipantStatuses WHERE Name = N'Accepted');
    SELECT m.Id AS MeetingId,
           m.Description,
           m.StartTime,
           m.Location,
           m.CreatedById,
           creator.FirstName AS CreatorFirstName,
           creator.LastName AS CreatorLastName,
           (SELECT COUNT(*) FROM dbo.MeetingParticipants mp WHERE mp.MeetingId = m.Id AND mp.StatusId = @AcceptedParticipantStatusId) AS ParticipantCount
    FROM dbo.Meetings m
    JOIN dbo.Users creator ON creator.Id = m.CreatedById
    WHERE m.Id = @MeetingId
      AND (
          m.CreatedById = @UserId
          OR EXISTS (SELECT 1 FROM dbo.MeetingParticipants mp WHERE mp.MeetingId = m.Id AND mp.ContactId = @UserId AND mp.StatusId = @AcceptedParticipantStatusId)
      );
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Meeting_Create
    @MeetingId UNIQUEIDENTIFIER,
    @StartTime DATETIME2,
    @Location NVARCHAR(100),
    @Description NVARCHAR(500),
    @CategoryId UNIQUEIDENTIFIER,
    @CreatedById UNIQUEIDENTIFIER
AS
BEGIN
    INSERT INTO dbo.Meetings (Id, StartTime, Location, Description, CategoryId, CreatedById)
    VALUES (@MeetingId, @StartTime, @Location, @Description, @CategoryId, @CreatedById);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Meeting_GetForEdit
    @MeetingId UNIQUEIDENTIFIER,
    @RequesterId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, StartTime, Location, Description, CategoryId, CreatedById
    FROM dbo.Meetings
    WHERE Id = @MeetingId AND CreatedById = @RequesterId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Meeting_GetDetails
    @MeetingId UNIQUEIDENTIFIER,
    @RequesterId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT m.Id,
           m.StartTime,
           m.Location,
           m.Description,
           m.CreatedById,
           CONCAT(creator.FirstName, ' ', creator.LastName) AS CreatedByName,
           m.CategoryId,
           cat.Name AS CategoryName,
           cat.Color AS CategoryColor,
           @RequesterId AS ViewerId,
           CASE WHEN m.CreatedById = @RequesterId THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS ViewerIsCreator,
           CASE WHEN m.CreatedById = @RequesterId OR EXISTS (SELECT 1 FROM dbo.MeetingParticipants vp WHERE vp.MeetingId = m.Id AND vp.ContactId = @RequesterId) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS ViewerIsParticipant,
           (SELECT TOP 1 ps.Name FROM dbo.MeetingParticipants vp JOIN dbo.ParticipantStatuses ps ON ps.Id = vp.StatusId WHERE vp.MeetingId = m.Id AND vp.ContactId = @RequesterId) AS ViewerStatus
    FROM dbo.Meetings m
    JOIN dbo.Users creator ON creator.Id = m.CreatedById
    LEFT JOIN dbo.Categories cat ON cat.Id = m.CategoryId
    WHERE m.Id = @MeetingId
      AND (m.CreatedById = @RequesterId OR EXISTS (SELECT 1 FROM dbo.MeetingParticipants vp WHERE vp.MeetingId = m.Id AND vp.ContactId = @RequesterId));
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Meeting_GetParticipants
    @MeetingId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT mp.ContactId,
           CONCAT(c.FirstName, ' ', c.LastName) AS DisplayName,
           c.Email,
           ps.Id AS StatusId,
           CASE WHEN mp.ContactId = m.CreatedById THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsCreator
    FROM dbo.MeetingParticipants mp
    JOIN dbo.Users c ON c.Id = mp.ContactId
    JOIN dbo.Meetings m ON m.Id = mp.MeetingId
    JOIN dbo.ParticipantStatuses ps ON ps.Id = mp.StatusId
    WHERE mp.MeetingId = @MeetingId
    ORDER BY CASE WHEN mp.ContactId = m.CreatedById THEN 0 ELSE 1 END, CONCAT(c.FirstName, ' ', c.LastName);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Meeting_GetOwnerId
    @MeetingId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CreatedById FROM dbo.Meetings WHERE Id = @MeetingId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Meeting_Update
    @MeetingId UNIQUEIDENTIFIER,
    @StartTime DATETIME2,
    @Location NVARCHAR(100),
    @Description NVARCHAR(500),
    @CategoryId UNIQUEIDENTIFIER
AS
BEGIN
    UPDATE dbo.Meetings
    SET StartTime = @StartTime,
        Location = @Location,
        Description = @Description,
        CategoryId = @CategoryId
    WHERE Id = @MeetingId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_MeetingParticipant_Upsert
    @MeetingId UNIQUEIDENTIFIER,
    @ContactId UNIQUEIDENTIFIER,
    @StatusId INT
AS
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.MeetingParticipants WHERE MeetingId = @MeetingId AND ContactId = @ContactId)
    BEGIN
        UPDATE dbo.MeetingParticipants
        SET StatusId = @StatusId
        WHERE MeetingId = @MeetingId AND ContactId = @ContactId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.MeetingParticipants (Id, MeetingId, ContactId, StatusId)
        VALUES (NEWID(), @MeetingId, @ContactId, @StatusId);
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_MeetingParticipant_GetByMeetingId
    @MeetingId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, MeetingId, ContactId, StatusId
    FROM dbo.MeetingParticipants
    WHERE MeetingId = @MeetingId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_MeetingParticipant_Delete
    @MeetingId UNIQUEIDENTIFIER,
    @ContactId UNIQUEIDENTIFIER
AS
BEGIN
    DELETE FROM dbo.MeetingParticipants
    WHERE MeetingId = @MeetingId AND ContactId = @ContactId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Meeting_SearchContacts
    @RequesterId UNIQUEIDENTIFIER,
    @SearchTerm NVARCHAR(256),
    @ExcludedIds NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Pattern NVARCHAR(260) = '%' + @SearchTerm + '%';
    SELECT c.Id,
           CONCAT(c.FirstName, ' ', c.LastName) AS DisplayName,
           c.Email
    FROM dbo.Users c
    WHERE c.Id <> @RequesterId
      AND (@ExcludedIds IS NULL OR NOT EXISTS (
            SELECT 1 FROM STRING_SPLIT(@ExcludedIds, ',') excluded
            WHERE TRY_CONVERT(uniqueidentifier, excluded.value) = c.Id
      ))
      AND (LOWER(c.FirstName) LIKE @Pattern OR LOWER(c.LastName) LIKE @Pattern OR LOWER(c.Email) LIKE @Pattern)
    ORDER BY c.FirstName, c.LastName;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Meeting_GetContacts
    @ExcludedIds NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.Id,
           CONCAT(c.FirstName, ' ', c.LastName) AS DisplayName,
           c.Email
    FROM dbo.Users c
    WHERE (@ExcludedIds IS NULL OR NOT EXISTS (
            SELECT 1 FROM STRING_SPLIT(@ExcludedIds, ',') excluded
            WHERE TRY_CONVERT(uniqueidentifier, excluded.value) = c.Id
      ))
    ORDER BY c.FirstName, c.LastName;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Meeting_GetMeetingsForUser
    @UserId UNIQUEIDENTIFIER,
    @SearchTerm NVARCHAR(256) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @AcceptedParticipantStatusId INT = (SELECT TOP 1 Id FROM dbo.ParticipantStatuses WHERE Name = N'Accepted');
    DECLARE @Pattern NVARCHAR(260) = '%' + @SearchTerm + '%';
    SELECT m.Id,
           m.StartTime,
           m.Location,
           m.Description,
           m.CreatedById,
           CONCAT(creator.FirstName, ' ', creator.LastName) AS CreatedByName,
           m.CategoryId,
           cat.Name AS CategoryName,
           cat.Color AS CategoryColor,
           CASE WHEN m.CreatedById = @UserId THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS ViewerIsCreator,
           (SELECT TOP 1 ps.Name FROM dbo.MeetingParticipants mp JOIN dbo.ParticipantStatuses ps ON ps.Id = mp.StatusId WHERE mp.MeetingId = m.Id AND mp.ContactId = @UserId) AS ViewerStatus,
           (SELECT COUNT(*) FROM dbo.MeetingParticipants mp2 WHERE mp2.MeetingId = m.Id) AS ParticipantCount
    FROM dbo.Meetings m
    JOIN dbo.Users creator ON creator.Id = m.CreatedById
    LEFT JOIN dbo.Categories cat ON cat.Id = m.CategoryId
    WHERE (m.CreatedById = @UserId OR EXISTS (SELECT 1 FROM dbo.MeetingParticipants mp WHERE mp.MeetingId = m.Id AND mp.ContactId = @UserId))
      AND (@SearchTerm IS NULL OR LOWER(ISNULL(m.Description, '')) LIKE @Pattern OR LOWER(ISNULL(m.Location, '')) LIKE @Pattern)
    ORDER BY m.StartTime DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Meeting_IsCreator
    @MeetingId UNIQUEIDENTIFIER,
    @ParticipantId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CAST(CASE WHEN EXISTS (
        SELECT 1 FROM dbo.Meetings WHERE Id = @MeetingId AND CreatedById = @ParticipantId
    ) THEN 1 ELSE 0 END AS bit);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_MeetingParticipant_UpdateStatus
    @MeetingId UNIQUEIDENTIFIER,
    @ContactId UNIQUEIDENTIFIER,
    @StatusId INT
AS
BEGIN
    UPDATE dbo.MeetingParticipants
    SET StatusId = @StatusId
    WHERE MeetingId = @MeetingId AND ContactId = @ContactId;
END
GO



CREATE OR ALTER PROCEDURE dbo.usp_MeetingParticipant_GetIds
    @MeetingId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ContactId FROM dbo.MeetingParticipants WHERE MeetingId = @MeetingId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_MeetingParticipant_GetNewlyAddedIds
    @MeetingId UNIQUEIDENTIFIER,
    @BeforeIds NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ContactId
    FROM dbo.MeetingParticipants mp
    WHERE mp.MeetingId = @MeetingId
      AND NOT EXISTS (
          SELECT 1 FROM STRING_SPLIT(@BeforeIds, ',') beforeids
          WHERE TRY_CONVERT(uniqueidentifier, beforeids.value) = mp.ContactId
      );
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Meeting_GetStartTime
    @MeetingId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT StartTime FROM dbo.Meetings WHERE Id = @MeetingId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Meeting_GetLocation
    @MeetingId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Location FROM dbo.Meetings WHERE Id = @MeetingId;
END
GO
