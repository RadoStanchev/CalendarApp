using CalendarApp.Infrastructure.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
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
            var hostEnvironment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            EnsureDatabaseExists(connectionString);

            var databaseDirectoryPath = Path.Combine(hostEnvironment.ContentRootPath, "database");
            var schemaFilePath = Path.Combine(databaseDirectoryPath, "schema.sql");
            var seedDefaultsFilePath = Path.Combine(databaseDirectoryPath, "seed-defaults.sql");

            EnsureSqlFileExists(schemaFilePath);
            EnsureSqlFileExists(seedDefaultsFilePath);

            if (!HasCoreSchema(connectionFactory))
            {
                ExecuteSqlFile(connectionFactory, schemaFilePath);
            }

            if (IsSeedDataMissing(connectionFactory))
            {
                ExecuteSqlFile(connectionFactory, seedDefaultsFilePath);
            }

            return app;
        }

        private static void EnsureSqlFileExists(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Could not find SQL file at '{filePath}'.", filePath);
            }
        }

        private static void EnsureDatabaseExists(string connectionString)
        {
            var targetBuilder = new SqlConnectionStringBuilder(connectionString);
            if (string.IsNullOrWhiteSpace(targetBuilder.InitialCatalog))
            {
                throw new InvalidOperationException("DefaultConnection must include a database name (Initial Catalog).");
            }

            var databaseName = targetBuilder.InitialCatalog;
            var masterBuilder = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = "master"
            };

            using var connection = new SqlConnection(masterBuilder.ConnectionString);
            connection.Open();

            var exists = connection.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM sys.databases WHERE [name] = @DatabaseName",
                new { DatabaseName = databaseName }) > 0;

            if (!exists)
            {
                var escapedDatabaseName = databaseName.Replace("]", "]]", StringComparison.Ordinal);
                connection.Execute($"CREATE DATABASE [{escapedDatabaseName}]");
            }
        }

        private static bool HasCoreSchema(IDbConnectionFactory connectionFactory)
        {
            using var connection = connectionFactory.CreateConnection();
            connection.Open();

            var contactsExists = connection.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM sys.tables WHERE [name] = 'Contacts' AND [schema_id] = SCHEMA_ID('dbo')") > 0;
            var categoriesExists = connection.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM sys.tables WHERE [name] = 'Categories' AND [schema_id] = SCHEMA_ID('dbo')") > 0;
            var meetingsExists = connection.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM sys.tables WHERE [name] = 'Meetings' AND [schema_id] = SCHEMA_ID('dbo')") > 0;

            return contactsExists && categoriesExists && meetingsExists;
        }

        private static bool IsSeedDataMissing(IDbConnectionFactory connectionFactory)
        {
            using var connection = connectionFactory.CreateConnection();
            connection.Open();

            var categoriesCount = connection.ExecuteScalar<int>("SELECT COUNT(1) FROM [dbo].[Categories]");
            var contactsCount = connection.ExecuteScalar<int>("SELECT COUNT(1) FROM [dbo].[Contacts]");
            var meetingsCount = connection.ExecuteScalar<int>("SELECT COUNT(1) FROM [dbo].[Meetings]");
            var meetingParticipantsCount = connection.ExecuteScalar<int>("SELECT COUNT(1) FROM [dbo].[MeetingParticipants]");

            return categoriesCount == 0
                || contactsCount == 0
                || meetingsCount == 0
                || meetingParticipantsCount == 0;
        }

        private static void ExecuteSqlFile(IDbConnectionFactory connectionFactory, string filePath)
        {
            var sql = File.ReadAllText(filePath);
            var batches = BatchSeparatorRegex.Split(sql)
                .Where(batch => !string.IsNullOrWhiteSpace(batch));

            using var connection = connectionFactory.CreateConnection();
            connection.Open();

            foreach (var batch in batches)
            {
                connection.Execute(batch);
            }
        }
    }
}
