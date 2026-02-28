using CalendarApp.Infrastructure.Data;
using CalendarApp.Services.Categories.Models;
using Dapper;

namespace CalendarApp.Services.Categories.Repositories;

public class DapperCategoryRepository : ICategoryRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public DapperCategoryRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<CategoryDetailsDto>> GetAllAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<CategoryDetailsDto>(@"SELECT c.Id, c.Name, c.Color, COUNT(m.Id) AS MeetingCount
FROM dbo.Categories c
LEFT JOIN dbo.Meetings m ON m.CategoryId = c.Id
GROUP BY c.Id, c.Name, c.Color
ORDER BY c.Name");
        return result.ToList();
    }

    public async Task<CategoryDetailsDto?> GetByIdAsync(Guid categoryId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<CategoryDetailsDto>(@"SELECT c.Id, c.Name, c.Color, COUNT(m.Id) AS MeetingCount
FROM dbo.Categories c
LEFT JOIN dbo.Meetings m ON m.CategoryId = c.Id
WHERE c.Id = @categoryId
GROUP BY c.Id, c.Name, c.Color", new { categoryId });
    }

    public async Task<Guid> CreateAsync(string name, string color)
    {
        var id = Guid.NewGuid();
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"INSERT INTO dbo.Categories (Id, Name, Color)
VALUES (@Id, @Name, @Color)", new { Id = id, Name = name, Color = color });
        return id;
    }

    public async Task<bool> UpdateAsync(Guid id, string name, string color)
    {
        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"UPDATE dbo.Categories SET Name = @Name, Color = @Color WHERE Id = @Id", new { Id = id, Name = name, Color = color });
        return affected > 0;
    }

    public async Task<int> CountAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM dbo.Categories");
    }

    public async Task<bool> DeleteAsync(Guid categoryId)
    {
        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync("DELETE FROM dbo.Categories WHERE Id = @categoryId", new { categoryId });
        return affected > 0;
    }

    public async Task<bool> IsInUseAsync(Guid categoryId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>("SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.Meetings WHERE CategoryId = @categoryId) THEN 1 ELSE 0 END AS bit)", new { categoryId });
    }
}
