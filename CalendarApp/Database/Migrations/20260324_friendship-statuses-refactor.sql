SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.FriendshipStatuses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FriendshipStatuses
    (
        Id INT NOT NULL CONSTRAINT PK_FriendshipStatuses PRIMARY KEY CLUSTERED,
        Name NVARCHAR(50) NOT NULL
    );
END;

MERGE dbo.FriendshipStatuses AS target
USING (VALUES
    (1, N'Pending'),
    (2, N'Accepted'),
    (3, N'Declined'),
    (4, N'Blocked')
) AS source (Id, Name)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET Name = source.Name
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, Name) VALUES (source.Id, source.Name);

IF COL_LENGTH('dbo.Friendships', 'StatusId') IS NULL
BEGIN
    ALTER TABLE dbo.Friendships ADD StatusId INT NULL;

    UPDATE dbo.Friendships
    SET StatusId =
        CASE Status
            WHEN 0 THEN 1 -- Pending
            WHEN 1 THEN 2 -- Accepted
            WHEN 2 THEN 3 -- Declined
            WHEN 3 THEN 4 -- Blocked
            ELSE 1
        END;

    DECLARE @DefaultName sysname;
    SELECT @DefaultName = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c
      ON c.object_id = dc.parent_object_id
     AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Friendships')
      AND c.name = N'Status';

    IF @DefaultName IS NOT NULL
    BEGIN
        EXEC(N'ALTER TABLE dbo.Friendships DROP CONSTRAINT ' + QUOTENAME(@DefaultName) + ';');
    END;

    ALTER TABLE dbo.Friendships DROP COLUMN Status;
    ALTER TABLE dbo.Friendships ALTER COLUMN StatusId INT NOT NULL;
    ALTER TABLE dbo.Friendships ADD CONSTRAINT DF_Friendships_StatusId DEFAULT ((1)) FOR StatusId;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_Friendships_FriendshipStatuses_StatusId'
      AND parent_object_id = OBJECT_ID(N'dbo.Friendships')
)
BEGIN
    ALTER TABLE dbo.Friendships WITH CHECK
    ADD CONSTRAINT FK_Friendships_FriendshipStatuses_StatusId
        FOREIGN KEY (StatusId) REFERENCES dbo.FriendshipStatuses(Id);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Friendships_StatusId'
      AND object_id = OBJECT_ID(N'dbo.Friendships')
)
BEGIN
    CREATE INDEX IX_Friendships_StatusId ON dbo.Friendships (StatusId);
END;

COMMIT TRANSACTION;
