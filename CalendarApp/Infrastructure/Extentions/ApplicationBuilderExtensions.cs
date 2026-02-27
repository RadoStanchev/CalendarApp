using CalendarApp.Data;
using CalendarApp.Data.Models;
using CalendarApp.Infrastructure.Time;
using Dapper;
using Microsoft.Data.SqlClient;
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

            SeedDefaults(connectionString);

            return app;
        }

        private static void SeedDefaults(string connectionString)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            EnsureCategories(connection, transaction);
            var contacts = EnsureDemoContacts(connection, transaction);
            var meetings = EnsureDemoMeetings(connection, transaction, contacts);
            EnsureMeetingParticipants(connection, transaction, contacts, meetings);

            transaction.Commit();
        }

        private static void EnsureCategories(SqlConnection connection, SqlTransaction transaction)
        {
            const string categoriesExistSql = "SELECT COUNT(*) FROM [dbo].[Categories]";
            var categoriesCount = connection.ExecuteScalar<int>(categoriesExistSql, transaction: transaction);

            if (categoriesCount > 0)
            {
                return;
            }

            var categories = new[]
            {
                new { Id = Guid.NewGuid(), Name = "Работа", Color = "#007BFF" },
                new { Id = Guid.NewGuid(), Name = "Лично", Color = "#28A745" },
                new { Id = Guid.NewGuid(), Name = "Рожден ден", Color = "#FFC107" },
                new { Id = Guid.NewGuid(), Name = "Семейство", Color = "#DC3545" },
                new { Id = Guid.NewGuid(), Name = "Образование", Color = "#6610F2" }
            };

            const string insertCategorySql = @"
                INSERT INTO [dbo].[Categories] ([Id], [Name], [Color])
                VALUES (@Id, @Name, @Color);";

            connection.Execute(insertCategorySql, categories, transaction);
        }

        private static Dictionary<string, Guid> EnsureDemoContacts(SqlConnection connection, SqlTransaction transaction)
        {
            const string contactsExistSql = "SELECT COUNT(*) FROM [dbo].[Contacts]";
            var contactsCount = connection.ExecuteScalar<int>(contactsExistSql, transaction: transaction);

            var contacts = new (string First, string Last, string Email)[]
            {
                ("Maria", "Ivanova", "maria@calendar.com"),
                ("Georgi", "Petrov", "georgi@calendar.com"),
                ("Elena", "Dimitrova", "elena@calendar.com"),
                ("Ivan", "Stoyanov", "ivan@calendar.com"),
                ("Nikolay", "Georgiev", "nikolay@calendar.com"),
                ("Petya", "Todorova", "petya@calendar.com"),
                ("Stefan", "Kolev", "stefan@calendar.com"),
                ("Tanya", "Mihaylova", "tanya@calendar.com"),
                ("Dimitar", "Krastev", "dimitar@calendar.com"),
                ("Админ", "Потребител", "admin@calendar.com")
            };

            if (contactsCount == 0)
            {
                const string insertContactSql = @"
                    INSERT INTO [dbo].[Contacts]
                        ([Id], [UserName], [Email], [EmailConfirmed], [PasswordHash], [SecurityStamp], [FirstName], [LastName], [BirthDate], [Address], [Note])
                    VALUES
                        (@Id, @UserName, @Email, @EmailConfirmed, @PasswordHash, @SecurityStamp, @FirstName, @LastName, @BirthDate, @Address, @Note);";

                var contactsToInsert = contacts.Select(contact => new
                {
                    Id = Guid.NewGuid(),
                    UserName = contact.Email,
                    Email = contact.Email,
                    EmailConfirmed = true,
                    PasswordHash = string.Empty,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    FirstName = contact.First,
                    LastName = contact.Last,
                    BirthDate = (DateTime?)null,
                    Address = (string?)null,
                    Note = (string?)null
                });

                connection.Execute(insertContactSql, contactsToInsert, transaction);
            }

            const string selectContactsSql = @"
                SELECT [Id], [Email]
                FROM [dbo].[Contacts]
                WHERE [Email] IN @Emails;";

            var selectedContacts = connection.Query<(Guid Id, string Email)>(
                selectContactsSql,
                new { Emails = contacts.Select(c => c.Email).ToArray() },
                transaction);

            return selectedContacts.ToDictionary(contact => contact.Email, contact => contact.Id, StringComparer.OrdinalIgnoreCase);
        }

        private static List<Guid> EnsureDemoMeetings(SqlConnection connection, SqlTransaction transaction, IReadOnlyDictionary<string, Guid> contacts)
        {
            const string meetingsExistSql = "SELECT COUNT(*) FROM [dbo].[Meetings]";
            var meetingsCount = connection.ExecuteScalar<int>(meetingsExistSql, transaction: transaction);

            if (meetingsCount > 0)
            {
                return connection.Query<Guid>(
                        "SELECT TOP (20) [Id] FROM [dbo].[Meetings] ORDER BY [StartTime] ASC;",
                        transaction: transaction)
                    .ToList();
            }

            var categories = connection.Query<(Guid Id, string Name)>(
                    "SELECT [Id], [Name] FROM [dbo].[Categories];",
                    transaction: transaction)
                .ToList();

            if (categories.Count == 0 || contacts.Count == 0)
            {
                return [];
            }

            var creatorIds = contacts.Values.ToArray();
            var random = new Random(42);
            var subjects = new[]
            {
                "Седмична синхронизация на екипа",
                "Презентация пред клиент",
                "Планиране на проект",
                "Преглед на код",
                "Маркетингова стратегия",
                "Демо на продукта",
                "Обсъждане на бюджет",
                "Мозъчна атака за UI/UX",
                "Преглед на представянето",
                "Тиймбилдинг събитие"
            };

            var meetings = new List<MeetingSeed>();
            for (int i = 0; i < 20; i++)
            {
                var category = categories[random.Next(categories.Count)];
                var creatorId = creatorIds[random.Next(creatorIds.Length)];
                var startLocal = BulgarianTime.LocalNow.AddDays(random.Next(-10, 10)).AddHours(random.Next(8, 18));

                meetings.Add(new MeetingSeed
                {
                    Id = Guid.NewGuid(),
                    StartTime = BulgarianTime.ConvertLocalToUtc(startLocal),
                    Location = $"Стая {random.Next(1, 5)}",
                    Description = subjects[random.Next(subjects.Length)],
                    CategoryId = category.Id,
                    CreatedById = creatorId,
                    ReminderSent = false
                });
            }

            const string insertMeetingsSql = @"
                INSERT INTO [dbo].[Meetings]
                    ([Id], [StartTime], [Location], [Description], [CategoryId], [CreatedById], [ReminderSent])
                VALUES
                    (@Id, @StartTime, @Location, @Description, @CategoryId, @CreatedById, @ReminderSent);";

            connection.Execute(insertMeetingsSql, meetings, transaction);

            return meetings.Select(m => m.Id).ToList();
        }

        private static void EnsureMeetingParticipants(
            SqlConnection connection,
            SqlTransaction transaction,
            IReadOnlyDictionary<string, Guid> contacts,
            IReadOnlyCollection<Guid> seededMeetingIds)
        {
            const string participantsExistSql = "SELECT COUNT(*) FROM [dbo].[MeetingParticipants]";
            var participantsCount = connection.ExecuteScalar<int>(participantsExistSql, transaction: transaction);

            if (participantsCount > 0)
            {
                return;
            }

            var meetings = seededMeetingIds.Count > 0
                ? seededMeetingIds.ToList()
                : connection.Query<Guid>("SELECT [Id] FROM [dbo].[Meetings];", transaction: transaction).ToList();

            if (meetings.Count == 0 || contacts.Count == 0)
            {
                return;
            }

            var contactIds = contacts.Values.ToList();
            var statuses = Enum.GetValues<ParticipantStatus>().Cast<int>().ToArray();
            var random = new Random(84);

            var participants = new List<object>();

            foreach (var meetingId in meetings)
            {
                var selectedContacts = contactIds
                    .OrderBy(_ => random.Next())
                    .Take(random.Next(5, Math.Min(9, contactIds.Count + 1)))
                    .ToList();

                foreach (var contactId in selectedContacts)
                {
                    participants.Add(new
                    {
                        Id = Guid.NewGuid(),
                        MeetingId = meetingId,
                        ContactId = contactId,
                        Status = statuses[random.Next(statuses.Length)]
                    });
                }
            }

            const string insertParticipantsSql = @"
                INSERT INTO [dbo].[MeetingParticipants]
                    ([Id], [MeetingId], [ContactId], [Status])
                VALUES
                    (@Id, @MeetingId, @ContactId, @Status);";

            connection.Execute(insertParticipantsSql, participants, transaction);
        }


        private sealed class MeetingSeed
        {
            public Guid Id { get; init; }
            public DateTime StartTime { get; init; }
            public string Location { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public Guid CategoryId { get; init; }
            public Guid CreatedById { get; init; }
            public bool ReminderSent { get; init; }
        }
    }
}
