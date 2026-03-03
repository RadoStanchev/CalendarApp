SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Domain schema only (no ASP.NET Identity role/claim/login tables and no EF migration metadata table).

CREATE TABLE dbo.Contacts (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Contacts_Id DEFAULT NEWID(),
    UserName NVARCHAR(256) NOT NULL,
    Email NVARCHAR(256) NOT NULL,
    EmailConfirmed BIT NOT NULL CONSTRAINT DF_Contacts_EmailConfirmed DEFAULT ((0)),
    PasswordHash NVARCHAR(1000) NOT NULL CONSTRAINT DF_Contacts_PasswordHash DEFAULT (N''),
    SecurityStamp NVARCHAR(100) NOT NULL CONSTRAINT DF_Contacts_SecurityStamp DEFAULT (N''),
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    BirthDate DATETIME2 NULL,
    Address NVARCHAR(100) NULL,
    Note NVARCHAR(250) NULL,
    CONSTRAINT PK_Contacts PRIMARY KEY CLUSTERED (Id ASC)
);
GO

CREATE TABLE dbo.Categories (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Categories_Id DEFAULT NEWID(),
    Name NVARCHAR(50) NOT NULL,
    Color NVARCHAR(20) NULL,
    CONSTRAINT PK_Categories PRIMARY KEY CLUSTERED (Id ASC)
);
GO

CREATE TABLE dbo.Friendships (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Friendships_Id DEFAULT NEWID(),
    RequesterId UNIQUEIDENTIFIER NOT NULL,
    ReceiverId UNIQUEIDENTIFIER NOT NULL,
    Status INT NOT NULL CONSTRAINT DF_Friendships_Status DEFAULT ((0)),
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Friendships_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Friendships PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT FK_Friendships_Contacts_RequesterId FOREIGN KEY (RequesterId) REFERENCES dbo.Contacts(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Friendships_Contacts_ReceiverId FOREIGN KEY (ReceiverId) REFERENCES dbo.Contacts(Id) ON DELETE NO ACTION
);
GO

CREATE TABLE dbo.Meetings (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Meetings_Id DEFAULT NEWID(),
    StartTime DATETIME2 NOT NULL,
    Location NVARCHAR(100) NULL,
    Description NVARCHAR(500) NULL,
    CategoryId UNIQUEIDENTIFIER NOT NULL,
    CreatedById UNIQUEIDENTIFIER NOT NULL,
    ReminderSent BIT NOT NULL CONSTRAINT DF_Meetings_ReminderSent DEFAULT ((0)),
    CONSTRAINT PK_Meetings PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT FK_Meetings_Categories_CategoryId FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Meetings_Contacts_CreatedById FOREIGN KEY (CreatedById) REFERENCES dbo.Contacts(Id) ON DELETE NO ACTION
);
GO

CREATE TABLE dbo.MeetingParticipants (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_MeetingParticipants_Id DEFAULT NEWID(),
    MeetingId UNIQUEIDENTIFIER NOT NULL,
    ContactId UNIQUEIDENTIFIER NOT NULL,
    Status INT NOT NULL CONSTRAINT DF_MeetingParticipants_Status DEFAULT ((0)),
    CONSTRAINT PK_MeetingParticipants PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT FK_MeetingParticipants_Meetings_MeetingId FOREIGN KEY (MeetingId) REFERENCES dbo.Meetings(Id) ON DELETE CASCADE,
    CONSTRAINT FK_MeetingParticipants_Contacts_ContactId FOREIGN KEY (ContactId) REFERENCES dbo.Contacts(Id) ON DELETE CASCADE
);
GO

CREATE TABLE dbo.Messages (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Messages_Id DEFAULT NEWID(),
    FriendshipId UNIQUEIDENTIFIER NULL,
    MeetingId UNIQUEIDENTIFIER NULL,
    SenderId UNIQUEIDENTIFIER NOT NULL,
    Content NVARCHAR(1000) NOT NULL,
    SentAt DATETIME2 NOT NULL CONSTRAINT DF_Messages_SentAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Messages PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT FK_Messages_Friendships_FriendshipId FOREIGN KEY (FriendshipId) REFERENCES dbo.Friendships(Id) ON DELETE SET NULL,
    CONSTRAINT FK_Messages_Meetings_MeetingId FOREIGN KEY (MeetingId) REFERENCES dbo.Meetings(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Messages_Contacts_SenderId FOREIGN KEY (SenderId) REFERENCES dbo.Contacts(Id) ON DELETE NO ACTION
);
GO

CREATE TABLE dbo.MessageSeens (
    MessageId UNIQUEIDENTIFIER NOT NULL,
    ContactId UNIQUEIDENTIFIER NOT NULL,
    SeenAt DATETIME2 NOT NULL CONSTRAINT DF_MessageSeens_SeenAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_MessageSeens PRIMARY KEY CLUSTERED (MessageId ASC, ContactId ASC),
    CONSTRAINT FK_MessageSeens_Messages_MessageId FOREIGN KEY (MessageId) REFERENCES dbo.Messages(Id) ON DELETE CASCADE,
    CONSTRAINT FK_MessageSeens_Contacts_ContactId FOREIGN KEY (ContactId) REFERENCES dbo.Contacts(Id) ON DELETE CASCADE
);
GO

CREATE TABLE dbo.Notifications (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Notifications_Id DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    Message NVARCHAR(200) NOT NULL,
    Type INT NOT NULL CONSTRAINT DF_Notifications_Type DEFAULT ((0)),
    IsRead BIT NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT ((0)),
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Notifications PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT FK_Notifications_Contacts_UserId FOREIGN KEY (UserId) REFERENCES dbo.Contacts(Id) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX IX_Contacts_Email ON dbo.Contacts (Email);
CREATE UNIQUE INDEX IX_Contacts_UserName ON dbo.Contacts (UserName);

CREATE INDEX IX_Friendships_RequesterId ON dbo.Friendships (RequesterId);
CREATE INDEX IX_Friendships_ReceiverId ON dbo.Friendships (ReceiverId);

CREATE INDEX IX_Meetings_CategoryId ON dbo.Meetings (CategoryId);
CREATE INDEX IX_Meetings_CreatedById ON dbo.Meetings (CreatedById);

CREATE INDEX IX_MeetingParticipants_MeetingId ON dbo.MeetingParticipants (MeetingId);
CREATE INDEX IX_MeetingParticipants_ContactId ON dbo.MeetingParticipants (ContactId);

CREATE INDEX IX_Messages_FriendshipId_SentAt ON dbo.Messages (FriendshipId, SentAt DESC);
CREATE INDEX IX_Messages_MeetingId_SentAt ON dbo.Messages (MeetingId, SentAt DESC);
CREATE INDEX IX_Messages_SenderId ON dbo.Messages (SenderId);

CREATE INDEX IX_MessageSeens_ContactId ON dbo.MessageSeens (ContactId);

CREATE INDEX IX_Notifications_UserId_CreatedAt ON dbo.Notifications (UserId, CreatedAt DESC);
CREATE INDEX IX_Notifications_UserId_IsRead ON dbo.Notifications (UserId, IsRead);
GO
