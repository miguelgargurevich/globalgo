using GlobalGo.Application.Interfaces;
using GlobalGo.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GlobalGo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICreditEvaluationService, CreditEvaluationService>();
        return services;
    }
}