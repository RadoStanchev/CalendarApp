CREATE OR ALTER PROCEDURE dbo.usp_Meeting_Create
    @MeetingId UNIQUEIDENTIFIER,
    @StartTime DATETIME2,
    @Location NVARCHAR(100),
    @Description NVARCHAR(500),
    @CategoryId UNIQUEIDENTIFIER,
    @CreatedById UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Meetings (Id, StartTime, Location, Description, CategoryId, CreatedById)
    VALUES (@MeetingId, @StartTime, @Location, @Description, @CategoryId, @CreatedById);
END
GO

CREATE PROCEDURE dbo.usp_MeetingParticipant_Upsert
    @MeetingId UNIQUEIDENTIFIER,
    @ContactId UNIQUEIDENTIFIER,
    @Status INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.MeetingParticipants WHERE MeetingId = @MeetingId AND ContactId = @ContactId)
    BEGIN
        UPDATE dbo.MeetingParticipants
        SET Status = @Status
        WHERE MeetingId = @MeetingId AND ContactId = @ContactId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.MeetingParticipants (Id, MeetingId, ContactId, Status)
        VALUES (NEWID(), @MeetingId, @ContactId, @Status);
    END
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
    SET NOCOUNT ON;

    UPDATE dbo.Meetings
    SET StartTime = @StartTime,
        Location = @Location,
        Description = @Description,
        CategoryId = @CategoryId
    WHERE Id = @MeetingId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_MeetingParticipant_Delete
    @MeetingId UNIQUEIDENTIFIER,
    @ContactId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.MeetingParticipants
    WHERE MeetingId = @MeetingId AND ContactId = @ContactId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_MeetingParticipant_UpdateStatus
    @MeetingId UNIQUEIDENTIFIER,
    @ContactId UNIQUEIDENTIFIER,
    @Status INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.MeetingParticipants
    SET Status = @Status
    WHERE MeetingId = @MeetingId AND ContactId = @ContactId;
END
GO

-- 1. GetAllAsync
CREATE OR ALTER PROCEDURE dbo.usp_Category_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.Id, c.Name, c.Color, COUNT(m.Id) AS MeetingCount
    FROM dbo.Categories c
    LEFT JOIN dbo.Meetings m ON m.CategoryId = c.Id
    GROUP BY c.Id, c.Name, c.Color
    ORDER BY c.Name;
END
GO

-- 2. GetByIdAsync
CREATE OR ALTER PROCEDURE dbo.usp_Category_GetById
    @categoryId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.Id, c.Name, c.Color, COUNT(m.Id) AS MeetingCount
    FROM dbo.Categories c
    LEFT JOIN dbo.Meetings m ON m.CategoryId = c.Id
    WHERE c.Id = @categoryId
    GROUP BY c.Id, c.Name, c.Color;
END
GO

-- 3. CreateAsync
CREATE OR ALTER PROCEDURE dbo.usp_Category_Create
    @Id UNIQUEIDENTIFIER,
    @Name NVARCHAR(50),
    @Color NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Categories (Id, Name, Color)
    VALUES (@Id, @Name, @Color);
END
GO

-- 4. UpdateAsync
CREATE OR ALTER PROCEDURE dbo.usp_Category_Update
    @Id UNIQUEIDENTIFIER,
    @Name NVARCHAR(50),
    @Color NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Categories 
    SET Name = @Name, Color = @Color 
    WHERE Id = @Id;
END
GO

-- 5. CountAsync
CREATE OR ALTER PROCEDURE dbo.usp_Category_Count
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM dbo.Categories;
END
GO

-- 6. DeleteAsync
CREATE OR ALTER PROCEDURE dbo.usp_Category_Delete
    @categoryId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.Categories WHERE Id = @categoryId;
END
GO

-- 7. IsInUseAsync
CREATE OR ALTER PROCEDURE dbo.usp_Category_IsInUse
    @categoryId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.Meetings WHERE CategoryId = @categoryId) THEN 1 ELSE 0 END AS bit);
END
GO