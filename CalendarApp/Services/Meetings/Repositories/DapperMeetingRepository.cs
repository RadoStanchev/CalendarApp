using CalendarApp.Infrastructure.Data;

namespace CalendarApp.Services.Meetings.Repositories;

public class DapperMeetingRepository : IMeetingRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public DapperMeetingRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }
}
