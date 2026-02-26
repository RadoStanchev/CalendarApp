using CalendarApp.Infrastructure.Data;

namespace CalendarApp.Services.Messages.Repositories;

public class DapperMessageRepository : IMessageRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public DapperMessageRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }
}
