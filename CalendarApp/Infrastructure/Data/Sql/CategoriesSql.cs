namespace CalendarApp.Infrastructure.Data.Sql;

public static class CategoriesSql
{
    public const string SelectAll = @"SELECT c.Id, c.Name, c.Color, COUNT(m.Id) AS MeetingCount
FROM dbo.Categories c
LEFT JOIN dbo.Meetings m ON m.CategoryId = c.Id
GROUP BY c.Id, c.Name, c.Color
ORDER BY c.Name";

    public const string SelectById = @"SELECT c.Id, c.Name, c.Color, COUNT(m.Id) AS MeetingCount
FROM dbo.Categories c
LEFT JOIN dbo.Meetings m ON m.CategoryId = c.Id
WHERE c.Id = @categoryId
GROUP BY c.Id, c.Name, c.Color";

    public const string Insert = @"INSERT INTO dbo.Categories (Id, Name, Color)
VALUES (@Id, @Name, @Color)";

    public const string Update = @"UPDATE dbo.Categories SET Name = @Name, Color = @Color WHERE Id = @Id";

    public const string Delete = "DELETE FROM dbo.Categories WHERE Id = @categoryId";

    public const string Count = "SELECT COUNT(1) FROM dbo.Categories";

    public const string HasMeetings = "SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.Meetings WHERE CategoryId = @categoryId) THEN 1 ELSE 0 END AS bit)";
}
