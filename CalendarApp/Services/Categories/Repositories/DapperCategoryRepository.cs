using CalendarApp.Infrastructure.Data;

namespace CalendarApp.Services.Categories.Repositories;

public class DapperCategoryRepository : ICategoryRepository
{
    private readonly IDbConnectionFactory connectionFactory;

    public DapperCategoryRepository(IDbConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }
}
