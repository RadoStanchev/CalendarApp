using CalendarApp.Services.Meetings.Models;
using CalendarApp.Infrastructure.Data;
using CalendarApp.Infrastructure.Time;
using CalendarApp.Services.Notifications;
using Dapper;
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
            var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var now = BulgarianTime.UtcNow;
            var windowEnd = now.Add(ReminderWindow);

            using var connection = connectionFactory.CreateConnection();

            var meetings = (await connection.QueryAsync<MeetingRecord>(
                "dbo.usp_Meeting_GetPendingReminders",
                new { Now = now, WindowEnd = windowEnd }, 
                commandType: System.Data.CommandType.StoredProcedure)).ToList();

            if (meetings.Count == 0)
            {
                return;
            }

            foreach (var meeting in meetings)
            {
                var recipientIds = (await connection.QueryAsync<Guid>(
                    "dbo.usp_MeetingParticipant_GetContactIds",
                    new { MeetingId = meeting.Id, DeclinedStatus = 2 }, 
                    commandType: System.Data.CommandType.StoredProcedure)).ToList();

                if (meeting.CreatedById != Guid.Empty && !recipientIds.Contains(meeting.CreatedById))
                {
                    recipientIds.Add(meeting.CreatedById);
                }

                if (recipientIds.Count > 0)
                {
                    await notificationService.SendMeetingReminderAsync(meeting, recipientIds);
                }

                await connection.ExecuteAsync(
                    "dbo.usp_Meeting_MarkReminderSent", 
                    new { MeetingId = meeting.Id }, 
                    commandType: System.Data.CommandType.StoredProcedure);
            }
        }
    }
}
