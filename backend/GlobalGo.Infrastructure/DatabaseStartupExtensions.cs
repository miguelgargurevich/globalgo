using GlobalGo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GlobalGo.Infrastructure;

public static class DatabaseStartupExtensions
{
    public static async Task EnsureInfrastructureDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CreditEvaluationDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }
}