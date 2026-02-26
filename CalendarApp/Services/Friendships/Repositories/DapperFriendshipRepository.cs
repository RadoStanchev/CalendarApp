using CalendarApp.Infrastructure.Data;

namespace CalendarApp.Services.Friendships.Repositories;

public class DapperFriendshipRepository : IFriendshipRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public DapperFriendshipRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }
}
