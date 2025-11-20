using CalendarApp.Data;
using CalendarApp.Data.Models;
using CalendarApp.Models;
using CalendarApp.Infrastructure.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CalendarApp.Infrastructure.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder PrepareDatabase(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var services = scope.ServiceProvider;

            var context = services.GetRequiredService<ApplicationDbContext>();
            context.Database.Migrate();

            SeedUsers(services);
            SeedCategories(services);
            SeedMeetingsWithParticipants(services);

            return app;
        }

        private static void SeedUsers(IServiceProvider services)
        {
            var userManager = services.GetRequiredService<UserManager<Contact>>();
            var context = services.GetRequiredService<ApplicationDbContext>();

            if (context.Users.Any()) return;

            var users = new (string First, string Last, string Email)[]
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
                ("Admin", "User", "admin@calendar.com")
            };

            foreach (var (first, last, email) in users)
            {
                var user = new Contact
                {
                    UserName = email,
                    Email = email,
                    FirstName = first,
                    LastName = last,
                    EmailConfirmed = true
                };

                userManager.CreateAsync(user, "Test123!").Wait();
            }
        }

        private static void SeedCategories(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            if (context.Categories.Any()) return;

            var categories = new[]
            {
                new Category { Name = "Work", Color = "#007BFF" },
                new Category { Name = "Personal", Color = "#28A745" },
                new Category { Name = "Birthday", Color = "#FFC107" },
                new Category { Name = "Family", Color = "#DC3545" },
                new Category { Name = "Education", Color = "#6610F2" }
            };

            context.Categories.AddRange(categories);
            context.SaveChanges();
        }

        private static void SeedMeetingsWithParticipants(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            if (context.Meetings.Any()) return;

            var users = context.Users.ToList();
            var categories = context.Categories.ToList();
            var random = new Random();

            var subjects = new[]
            {
                "Weekly Team Sync",
                "Client Presentation",
                "Project Planning",
                "Code Review",
                "Marketing Strategy",
                "Product Demo",
                "Budget Discussion",
                "UI/UX Brainstorm",
                "Performance Review",
                "Team Building Event"
            };

            var meetings = new List<Meeting>();

            // Create 20 meetings
            for (int i = 0; i < 20; i++)
            {
                var creator = users[random.Next(users.Count)];
                var category = categories[random.Next(categories.Count)];

                var startLocal = BulgarianTime.LocalNow.AddDays(random.Next(-10, 10)).AddHours(random.Next(8, 18));
                var startUtc = BulgarianTime.ConvertLocalToUtc(startLocal);

                var meeting = new Meeting
                {
                    Description = subjects[random.Next(subjects.Length)],
                    Location = $"Room {random.Next(1, 5)}",
                    StartTime = startUtc,
                    CategoryId = category.Id,
                    CreatedById = creator.Id
                };

                meetings.Add(meeting);
            }

            context.Meetings.AddRange(meetings);
            context.SaveChanges();

            // Now seed meeting participants
            var allMeetings = context.Meetings.ToList();
            var participantStatuses = Enum.GetValues<ParticipantStatus>();

            foreach (var meeting in allMeetings)
            {
                var selectedUsers = users
                    .OrderBy(_ => random.Next())
                    .Take(random.Next(5, 9)) // 5 to 8 participants per meeting
                    .ToList();

                foreach (var user in selectedUsers)
                {
                    var participant = new MeetingParticipant
                    {
                        MeetingId = meeting.Id,
                        ContactId = user.Id,
                        Status = participantStatuses[random.Next(participantStatuses.Length)]
                    };

                    context.MeetingParticipants.Add(participant);
                }
            }

            context.SaveChanges();
        }
    }
}
