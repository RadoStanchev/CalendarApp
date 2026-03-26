using System.Data;
using CalendarApp.Infrastructure.Data;
using CalendarApp.Services.User.Models;
using Dapper;

namespace CalendarApp.Repositories.User;

public class DapperUserRepository : IUserRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public DapperUserRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<bool> CreateAsync(UserRecord user)
    {
        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync("dbo.usp_User_Create", user, commandType: CommandType.StoredProcedure);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync("dbo.usp_User_Delete", new { Id = id }, commandType: CommandType.StoredProcedure);
        return affected > 0;
    }

    public async Task<IEnumerable<UserRecord>> GetAllAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<UserRecord>("dbo.usp_User_GetAll", commandType: CommandType.StoredProcedure);
    }

    public async Task<UserRecord?> GetByEmailAsync(string email)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<UserRecord>("dbo.usp_User_GetByEmail", new { Email = email }, commandType: CommandType.StoredProcedure);
    }

    public async Task<UserRecord?> GetByIdAsync(Guid id)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<UserRecord>("dbo.usp_User_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
    }

    public async Task<string?> GetFullNameAsync(Guid id)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<string>("dbo.usp_User_GetFullName", new { Id = id }, commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<UserRecord>> SearchAsync(string term)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<UserRecord>("dbo.usp_User_Search", new { Term = term.Trim().ToLowerInvariant() }, commandType: CommandType.StoredProcedure);
    }

    public async Task<bool> UpdateProfileAsync(UpdateProfileDto user)
    {
        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync("dbo.usp_User_UpdateProfile", user, commandType: CommandType.StoredProcedure);
        return affected > 0;
    }
}
