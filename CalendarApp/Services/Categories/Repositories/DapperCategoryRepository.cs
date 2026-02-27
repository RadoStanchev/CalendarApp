using CalendarApp.Infrastructure.Data;
using CalendarApp.Infrastructure.Data.Sql;
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
        var result = await connection.QueryAsync<CategoryDetailsDto>(CategoriesSql.SelectAll);
        return result.ToList();
    }

    public async Task<CategoryDetailsDto?> GetByIdAsync(Guid categoryId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<CategoryDetailsDto>(CategoriesSql.SelectById, new { categoryId });
    }

    public async Task<Guid> CreateAsync(string name, string color)
    {
        var id = Guid.NewGuid();
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(CategoriesSql.Insert, new { Id = id, Name = name, Color = color });
        return id;
    }

    public async Task<bool> UpdateAsync(Guid id, string name, string color)
    {
        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(CategoriesSql.Update, new { Id = id, Name = name, Color = color });
        return affected > 0;
    }

    public async Task<int> CountAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(CategoriesSql.Count);
    }

    public async Task<bool> DeleteAsync(Guid categoryId)
    {
        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(CategoriesSql.Delete, new { categoryId });
        return affected > 0;
    }

    public async Task<bool> IsInUseAsync(Guid categoryId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(CategoriesSql.HasMeetings, new { categoryId });
    }
}
