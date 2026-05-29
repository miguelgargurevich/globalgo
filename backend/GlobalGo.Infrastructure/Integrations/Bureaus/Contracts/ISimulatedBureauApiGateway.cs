using GlobalGo.Application.Interfaces;

namespace GlobalGo.Infrastructure.Integrations.Bureaus.Contracts;

public interface ISimulatedBureauApiGateway
{
    Task<TResponse> RequestAsync<TResponse>(
        string sourceCode,
        CreditSourceContext context,
        Func<int, TResponse> responseFactory,
        CancellationToken cancellationToken = default);
}
