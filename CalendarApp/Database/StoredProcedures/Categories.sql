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

CREATE OR ALTER PROCEDURE dbo.usp_Category_Count
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM dbo.Categories;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Category_Delete
    @categoryId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.Categories WHERE Id = @categoryId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Category_IsInUse
    @categoryId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.Meetings WHERE CategoryId = @categoryId) THEN 1 ELSE 0 END AS bit);
END
GO
