using Dapper;
using System.Text.RegularExpressions;
using CalendarApp.Infrastructure.Data;

namespace CalendarApp.Infrastructure.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        private static readonly Regex BatchSeparatorRegex = new(@"^\s*GO\s*(?:--.*)?$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        public static IApplicationBuilder PrepareDatabase(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();

            var databaseDirectory = FindDatabaseDirectory();

            ExecuteSqlFile(connectionFactory, Path.Combine(databaseDirectory, "schema.sql"));
            ExecuteSqlFile(connectionFactory, Path.Combine(databaseDirectory, "seed-defaults.sql"));

            return app;
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

        private static string FindDatabaseDirectory()
        {
            var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

            while (currentDirectory is not null)
            {
                var candidateDirectory = Path.Combine(currentDirectory.FullName, "database");
                var schemaFilePath = Path.Combine(candidateDirectory, "schema.sql");
                var seedFilePath = Path.Combine(candidateDirectory, "seed-defaults.sql");

                if (File.Exists(schemaFilePath) && File.Exists(seedFilePath))
                {
                    return candidateDirectory;
                }

                currentDirectory = currentDirectory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate a database directory containing schema.sql and seed-defaults.sql.");
        }
    }
}
