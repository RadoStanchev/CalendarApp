SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;


IF NOT EXISTS (SELECT * FROM dbo.FriendshipStatuses)
BEGIN
    INSERT INTO dbo.FriendshipStatuses (Id, Name)
    VALUES
        (1, N'Pending'),
        (2, N'Accepted'),
        (3, N'Declined'),
        (4, N'Blocked');
END;

IF NOT EXISTS (SELECT * FROM dbo.Categories)
BEGIN
    INSERT INTO dbo.Categories (Id, Name, Color)
    VALUES
        ('A1111111-1111-1111-1111-111111111111', N'Работа', '#007BFF'),
        ('A2222222-2222-2222-2222-222222222222', N'Лично', '#28A745'),
        ('A3333333-3333-3333-3333-333333333333', N'Рожден ден', '#FFC107'),
        ('A4444444-4444-4444-4444-444444444444', N'Семейство', '#DC3545'),
        ('A5555555-5555-5555-5555-555555555555', N'Образование', '#6610F2');
END;

IF NOT EXISTS (SELECT * FROM dbo.Contacts)
BEGIN
    INSERT INTO dbo.Contacts
        (Id, UserName, Email, EmailConfirmed, PasswordHash, FirstName, LastName, BirthDate, Address, Note)
    VALUES
        (NEWID(), 'maria@calendar.com', 'maria@calendar.com', 1, N'', 'Maria', 'Ivanova', NULL, NULL, NULL),
        (NEWID(), 'georgi@calendar.com', 'georgi@calendar.com', 1, N'', 'Georgi', 'Petrov', NULL, NULL, NULL),
        (NEWID(), 'elena@calendar.com', 'elena@calendar.com', 1, N'', 'Elena', 'Dimitrova', NULL, NULL, NULL),
        (NEWID(), 'ivan@calendar.com', 'ivan@calendar.com', 1, N'', 'Ivan', 'Stoyanov', NULL, NULL, NULL),
        (NEWID(), 'nikolay@calendar.com', 'nikolay@calendar.com', 1, N'', 'Nikolay', 'Georgiev', NULL, NULL, NULL),
        (NEWID(), 'petya@calendar.com', 'petya@calendar.com', 1, N'', 'Petya', 'Todorova', NULL, NULL, NULL),
        (NEWID(), 'stefan@calendar.com', 'stefan@calendar.com', 1, N'', 'Stefan', 'Kolev', NULL, NULL, NULL),
        (NEWID(), 'tanya@calendar.com', 'tanya@calendar.com', 1, N'', 'Tanya', 'Mihaylova', NULL, NULL, NULL),
        (NEWID(), 'dimitar@calendar.com', 'dimitar@calendar.com', 1, N'', 'Dimitar', 'Krastev', NULL, NULL, NULL),
        (NEWID(), 'admin@calendar.com', 'admin@calendar.com', 1, N'', N'Админ', N'Потребител', NULL, NULL, NULL);
END;

IF NOT EXISTS (SELECT * FROM dbo.Meetings)
BEGIN
    ;WITH SeedRows(Idx, Subject) AS
    (
        SELECT *
        FROM (VALUES
            (0, N'Седмична синхронизация на екипа'),
            (1, N'Презентация пред клиент'),
            (2, N'Планиране на проект'),
            (3, N'Преглед на код'),
            (4, N'Маркетингова стратегия'),
            (5, N'Демо на продукта'),
            (6, N'Обсъждане на бюджет'),
            (7, N'Мозъчна атака за UI/UX'),
            (8, N'Преглед на представянето'),
            (9, N'Тиймбилдинг събитие'),
            (10, N'Седмична синхронизация на екипа'),
            (11, N'Презентация пред клиент'),
            (12, N'Планиране на проект'),
            (13, N'Преглед на код'),
            (14, N'Маркетингова стратегия'),
            (15, N'Демо на продукта'),
            (16, N'Обсъждане на бюджет'),
            (17, N'Мозъчна атака за UI/UX'),
            (18, N'Преглед на представянето'),
            (19, N'Тиймбилдинg събитие')
        ) AS v(Idx, Subject)
    ),
    CategoryRows AS
    (
        SELECT Id, ROW_NUMBER() OVER (ORDER BY Name) - 1 AS Idx
        FROM dbo.Categories
    ),
    ContactRows AS
    (
        SELECT Id, ROW_NUMBER() OVER (ORDER BY Email) - 1 AS Idx
        FROM dbo.Contacts
    )
    INSERT INTO dbo.Meetings
        (Id, StartTime, Location, Description, CategoryId, CreatedById, ReminderSent)
    SELECT
        NEWID(),
        DATEADD(HOUR, 8 + (s.Idx % 10), DATEADD(DAY, s.Idx - 10, SYSUTCDATETIME())),
        CONCAT(N'Стая ', (s.Idx % 4) + 1),
        s.Subject,
        c.Id,
        u.Id,
        0
    FROM SeedRows AS s
    CROSS APPLY (
        SELECT TOP 1 Id
        FROM CategoryRows
        WHERE Idx = s.Idx % NULLIF((SELECT COUNT(*) FROM CategoryRows), 0)
    ) AS c
    CROSS APPLY (
        SELECT TOP 1 Id
        FROM ContactRows
        WHERE Idx = s.Idx % NULLIF((SELECT COUNT(*) FROM ContactRows), 0)
    ) AS u;
END;

IF NOT EXISTS (SELECT * FROM dbo.MeetingParticipants)
BEGIN
    ;WITH MeetingRows AS
    (
        SELECT Id, ROW_NUMBER() OVER (ORDER BY StartTime, Id) AS MeetingNo
        FROM dbo.Meetings
    ),
    ContactRows AS
    (
        SELECT Id, ROW_NUMBER() OVER (ORDER BY Email) AS ContactNo
        FROM dbo.Contacts
    )
    INSERT INTO dbo.MeetingParticipants (Id, MeetingId, ContactId, Status)
    SELECT
        NEWID(),
        m.Id,
        c.Id,
        (m.MeetingNo + c.ContactNo) % 3
    FROM MeetingRows AS m
    INNER JOIN ContactRows AS c
        ON c.ContactNo BETWEEN 1 AND 4;
END;

COMMIT TRANSACTION;
