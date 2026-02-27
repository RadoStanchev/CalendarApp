using CalendarApp.Data;
using Microsoft.EntityFrameworkCore;

namespace CalendarApp.Infrastructure.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder PrepareDatabase(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var connectionString = context.Database.GetConnectionString();

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Database connection string is not configured.");
            }

            return app;
        }
    }
}
