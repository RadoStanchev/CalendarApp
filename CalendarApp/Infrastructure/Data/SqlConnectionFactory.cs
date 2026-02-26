using Microsoft.Data.SqlClient;
using System.Data;

namespace CalendarApp.Infrastructure.Data;

public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public IDbConnection CreateConnection() => new SqlConnection(connectionString);
}
