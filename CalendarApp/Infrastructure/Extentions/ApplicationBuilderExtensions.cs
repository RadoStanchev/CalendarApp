using CalendarApp.Infrastructure.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace CalendarApp.Infrastructure.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        private static readonly Regex BatchSeparatorRegex = new(@"^\s*GO\s*(?:--.*)?$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        public static IApplicationBuilder PrepareDatabase(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
            var connectionString = configuration.GetConnectionString("DefaultConnection")!;

            EnsureDatabaseExists(connectionString);

            var databaseDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "Database");
            var schemaFilePath = Path.Combine(databaseDirectoryPath, "schema.sql");
            var seedDefaultsFilePath = Path.Combine(databaseDirectoryPath, "seed-defaults.sql");
            var storedProceduresDirectoryPath = Path.Combine(databaseDirectoryPath, "StoredProcedures");

            if (File.Exists(schemaFilePath) && IsDatabaseEmpty(connectionFactory))
            {
                ExecuteSqlFile(connectionFactory, schemaFilePath);
            }

            if (Directory.Exists(storedProceduresDirectoryPath))
            {
                foreach (var storedProcedureFilePath in Directory.GetFiles(storedProceduresDirectoryPath, "*.sql", SearchOption.TopDirectoryOnly).OrderBy(path => path))
                {
                    ExecuteSqlFile(connectionFactory, storedProcedureFilePath);
                }
            }

            if (File.Exists(seedDefaultsFilePath) && IsSeedDataMissing(connectionFactory))
            {
                ExecuteSqlFile(connectionFactory, seedDefaultsFilePath);
            }

            return app;
        }

        private static void EnsureDatabaseExists(string connectionString)
        {
            var targetBuilder = new SqlConnectionStringBuilder(connectionString);
            var databaseName = targetBuilder.InitialCatalog;
            targetBuilder.InitialCatalog = "master";

            using var connection = new SqlConnection(targetBuilder.ConnectionString);
            connection.Open();

            if (connection.ExecuteScalar<int>("SELECT COUNT(*) FROM sys.databases WHERE name = @DatabaseName", new { DatabaseName = databaseName }) == 0)
            {
                connection.Execute($"CREATE DATABASE {databaseName}");
            }
        }

        private static bool IsDatabaseEmpty(IDbConnectionFactory connectionFactory)
        {
            using var connection = connectionFactory.CreateConnection();
            return connection.ExecuteScalar<int>("SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0") == 0;
        }

        private static bool IsSeedDataMissing(IDbConnectionFactory connectionFactory)
        {
            using var connection = connectionFactory.CreateConnection();
            return connection.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.Categories") == 0
                || connection.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.Contacts") == 0
                || connection.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.Meetings") == 0
                || connection.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.MeetingParticipants") == 0;
        }

        private static void ExecuteSqlFile(IDbConnectionFactory connectionFactory, string filePath)
        {
            var sql = File.ReadAllText(filePath);
            var batches = BatchSeparatorRegex.Split(sql)
                .Where(batch => !string.IsNullOrWhiteSpace(batch));

            using var connection = connectionFactory.CreateConnection();

            foreach (var batch in batches)
            {
                connection.Execute(batch);
            }
        }
    }
}
