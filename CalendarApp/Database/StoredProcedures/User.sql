CREATE OR ALTER PROCEDURE dbo.usp_User_Create
    @Id UNIQUEIDENTIFIER,
    @Email NVARCHAR(256),
    @EmailConfirmed BIT,
    @PasswordHash NVARCHAR(MAX),
    @FirstName NVARCHAR(100),
    @LastName NVARCHAR(100),
    @BirthDate DATETIME2,
    @Address NVARCHAR(500),
    @Note NVARCHAR(MAX)
AS
BEGIN
    INSERT INTO dbo.Users (Id, Email, EmailConfirmed, PasswordHash, FirstName, LastName, BirthDate, Address, Note)
    VALUES (@Id, @Email, @EmailConfirmed, @PasswordHash, @FirstName, @LastName, @BirthDate, @Address, @Note);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_Delete
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    DELETE FROM dbo.Users WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.Users ORDER BY FirstName, LastName;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_GetByEmail
    @Email NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 * FROM dbo.Users WHERE Email = @Email;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_GetById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 * FROM dbo.Users WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_Search
    @Term NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Pattern NVARCHAR(260) = '%' + @Term + '%';
    SELECT * FROM dbo.Users
    WHERE LOWER(FirstName) LIKE @Pattern OR LOWER(LastName) LIKE @Pattern OR LOWER(Email) LIKE @Pattern
    ORDER BY FirstName;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_UpdateProfile
    @Id UNIQUEIDENTIFIER,
    @FirstName NVARCHAR(100),
    @LastName NVARCHAR(100),
    @BirthDate DATETIME2,
    @Address NVARCHAR(500),
    @Note NVARCHAR(MAX)
AS
BEGIN
    UPDATE dbo.Users
    SET FirstName = @FirstName,
        LastName = @LastName,
        BirthDate = @BirthDate,
        [Address] = @Address,
        Note = @Note
    WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_GetFullName
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 CONCAT(FirstName, ' ', LastName) AS FullName
    FROM dbo.Users
    WHERE Id = @Id;
END
GO
