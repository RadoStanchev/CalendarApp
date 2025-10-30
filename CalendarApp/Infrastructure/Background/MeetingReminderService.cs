using CalendarApp.Data;
using CalendarApp.Data.Models;
using CalendarApp.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CalendarApp.Infrastructure.Background
{
    public class MeetingReminderService : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan ReminderWindow = TimeSpan.FromHours(1);

        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<MeetingReminderService> logger;

        public MeetingReminderService(IServiceProvider serviceProvider, ILogger<MeetingReminderService> logger)
        {
            this.serviceProvider = serviceProvider;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Meeting reminder service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckForUpcomingMeetingsAsync(stoppingToken);
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while processing meeting reminders.");
                }
            }

            logger.LogInformation("Meeting reminder service stopped.");
        }

        private async Task CheckForUpcomingMeetingsAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var now = DateTime.Now;
            var windowEnd = now.Add(ReminderWindow);

            var meetings = await db.Meetings
                .Include(m => m.Participants)
                .Where(m => !m.ReminderSent && m.StartTime >= now && m.StartTime <= windowEnd)
                .ToListAsync(cancellationToken);

            if (meetings.Count == 0)
                return;

            foreach (var meeting in meetings)
            {
                var recipientIds = meeting.Participants
                    .Where(p => p.Status != ParticipantStatus.Declined)
                    .Select(p => p.ContactId)
                    .Distinct()
                    .ToList();

                if (meeting.CreatedById != Guid.Empty && !recipientIds.Contains(meeting.CreatedById))
                {
                    recipientIds.Add(meeting.CreatedById);
                }

                if (recipientIds.Count == 0)
                {
                    meeting.ReminderSent = true;
                    continue;
                }

                await notificationService.SendMeetingReminderAsync(meeting, recipientIds, cancellationToken);
                meeting.ReminderSent = true;
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
