using System.Data;
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
        var result = await connection.QueryAsync<CategoryDetailsDto>(
            "dbo.usp_Category_GetAll", 
            commandType: CommandType.StoredProcedure);
        return result.ToList();
    }

    public async Task<CategoryDetailsDto?> GetByIdAsync(Guid categoryId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<CategoryDetailsDto>(
            "dbo.usp_Category_GetById", 
            new { categoryId }, 
            commandType: CommandType.StoredProcedure);
    }

    public async Task<Guid> CreateAsync(string name, string color)
    {
        var id = Guid.NewGuid();
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "dbo.usp_Category_Create", 
            new { Id = id, Name = name, Color = color }, 
            commandType: CommandType.StoredProcedure);
        return id;
    }

    public async Task<bool> UpdateAsync(Guid id, string name, string color)
    {
        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "dbo.usp_Category_Update", 
            new { Id = id, Name = name, Color = color }, 
            commandType: CommandType.StoredProcedure);
        return affected > 0;
    }

    public async Task<int> CountAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            "dbo.usp_Category_Count", 
            commandType: CommandType.StoredProcedure);
    }

    public async Task<bool> DeleteAsync(Guid categoryId)
    {
        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "dbo.usp_Category_Delete", 
            new { categoryId }, 
            commandType: CommandType.StoredProcedure);
        return affected > 0;
    }

    public async Task<bool> IsInUseAsync(Guid categoryId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(
            "dbo.usp_Category_IsInUse", 
            new { categoryId }, 
            commandType: CommandType.StoredProcedure);
    }
}