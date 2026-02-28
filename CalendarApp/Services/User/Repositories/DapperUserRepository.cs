using CalendarApp.Services.User.Models;
using CalendarApp.Infrastructure.Data;
using Dapper;

namespace CalendarApp.Services.User.Repositories;

public class DapperUserRepository : IUserRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public DapperUserRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<bool> CreateAsync(UserRecord user)
    {
        const string sql = @"INSERT INTO dbo.Contacts
(Id, UserName, Email, EmailConfirmed, PasswordHash, SecurityStamp, FirstName, LastName, BirthDate, Address, Note)
VALUES (@Id, @UserName, @Email, @EmailConfirmed, @PasswordHash, @SecurityStamp, @FirstName, @LastName, @BirthDate, @Address, @Note)";

        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(sql, user);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync("DELETE FROM dbo.Contacts WHERE Id = @id", new { id });
        return affected > 0;
    }

    public async Task<IEnumerable<UserRecord>> GetAllAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<UserRecord>("SELECT * FROM dbo.Contacts ORDER BY FirstName");
    }

    public async Task<UserRecord?> GetByEmailAsync(string email)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<UserRecord>("SELECT TOP 1 * FROM dbo.Contacts WHERE Email = @email", new { email });
    }

    public async Task<UserRecord?> GetByIdAsync(Guid id)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<UserRecord>("SELECT TOP 1 * FROM dbo.Contacts WHERE Id = @id", new { id });
    }

    public async Task<IEnumerable<UserRecord>> SearchAsync(string term)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = @"SELECT * FROM dbo.Contacts
WHERE LOWER(FirstName) LIKE @term OR LOWER(LastName) LIKE @term OR LOWER(Email) LIKE @term
ORDER BY FirstName";

        return await connection.QueryAsync<UserRecord>(sql, new { term = $"%{term.ToLower()}%" });
    }

    public async Task<bool> UpdateProfileAsync(UserRecord user)
    {
        const string sql = @"UPDATE dbo.Contacts
SET FirstName = @FirstName,
    LastName = @LastName,
    BirthDate = @BirthDate,
    Address = @Address,
    Note = @Note
WHERE Id = @Id";

        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(sql, user);
        return affected > 0;
    }
}
