using CalendarApp.Infrastructure.Data;

namespace CalendarApp.Services.MessageSeens.Repositories;

public class DapperMessageSeenRepository : IMessageSeenRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public DapperMessageSeenRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }
}
