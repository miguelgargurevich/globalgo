using GlobalGo.Application.Interfaces;
using GlobalGo.Domain.Entities;
using GlobalGo.Infrastructure.Integrations.Bureaus.Contracts;

namespace GlobalGo.Infrastructure.Integrations.Bureaus.Providers;

public sealed class SimulatedReniecClient : ICreditSourceProvider
{
    private readonly ISimulatedBureauApiGateway _apiGateway;

    public SimulatedReniecClient(ISimulatedBureauApiGateway apiGateway)
    {
        _apiGateway = apiGateway;
    }

    public string SourceCode => CreditSourceCodes.Reniec;

    public async Task<object> GetReportAsync(CreditSourceContext context, CancellationToken cancellationToken = default)
    {
        var response = await _apiGateway.RequestAsync(
            SourceCode,
            context,
            seed =>
            {
                var identityValid = (seed % 20) != 0;
                var isDeceased = (seed % 97) == 0;
                return new ReniecReport(identityValid, isDeceased);
            },
            cancellationToken);

        return response;
    }
}
