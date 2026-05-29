using GlobalGo.Application.Interfaces;
using GlobalGo.Infrastructure.Integrations.Bureaus.Contracts;
using GlobalGo.Infrastructure.Integrations.Bureaus.Gateway;
using GlobalGo.Infrastructure.Integrations.Bureaus.Providers;
using GlobalGo.Infrastructure.Persistence;
using GlobalGo.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GlobalGo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres no está configurado.");

        services.AddDbContext<CreditEvaluationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ICreditEvaluationRepository, PostgresCreditEvaluationRepository>();
        services.AddSingleton<ISimulatedBureauApiGateway, SimulatedBureauApiGateway>();
        services.AddSingleton<ICreditSourceProvider, SimulatedEquifaxClient>();
        services.AddSingleton<ICreditSourceProvider, SimulatedReniecClient>();
        services.AddSingleton<ICreditSourceProvider, SimulatedSbsClient>();

        return services;
    }
}