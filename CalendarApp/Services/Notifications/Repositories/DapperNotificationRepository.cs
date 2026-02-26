using CalendarApp.Infrastructure.Data;

namespace CalendarApp.Services.Notifications.Repositories;

public class DapperNotificationRepository : INotificationRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public DapperNotificationRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }
}
