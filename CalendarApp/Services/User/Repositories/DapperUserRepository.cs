using CalendarApp.Data.Models;
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

    public async Task<bool> CreateAsync(Contact user)
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

    public async Task<IEnumerable<Contact>> GetAllAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<Contact>("SELECT * FROM dbo.Contacts ORDER BY FirstName");
    }

    public async Task<Contact?> GetByEmailAsync(string email)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Contact>("SELECT TOP 1 * FROM dbo.Contacts WHERE Email = @email", new { email });
    }

    public async Task<Contact?> GetByIdAsync(Guid id)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Contact>("SELECT TOP 1 * FROM dbo.Contacts WHERE Id = @id", new { id });
    }

    public async Task<IEnumerable<Contact>> SearchAsync(string term)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = @"SELECT * FROM dbo.Contacts
WHERE LOWER(FirstName) LIKE @term OR LOWER(LastName) LIKE @term OR LOWER(Email) LIKE @term
ORDER BY FirstName";

        return await connection.QueryAsync<Contact>(sql, new { term = $"%{term.ToLower()}%" });
    }

    public async Task<bool> UpdateProfileAsync(Contact user)
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
