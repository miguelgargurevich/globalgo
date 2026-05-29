using GlobalGo.Application.Interfaces;
using GlobalGo.Domain.Entities;

namespace GlobalGo.Infrastructure.Bureaus;

public sealed class SimulatedEquifaxClient : ICreditSourceProvider
{
    private readonly ISimulatedBureauApiGateway _apiGateway;

    public SimulatedEquifaxClient(ISimulatedBureauApiGateway apiGateway)
    {
        _apiGateway = apiGateway;
    }

    public string SourceCode => CreditSourceCodes.Equifax;

    public async Task<object> GetReportAsync(CreditSourceContext context, CancellationToken cancellationToken = default)
    {
        var response = await _apiGateway.RequestAsync(
            SourceCode,
            context,
            seed =>
            {
                var score = 350 + (seed % 551);
                var latePayments = seed % 6;
                var hasDelinquency = latePayments >= 3;
                return new EquifaxReport(score, hasDelinquency, latePayments);
            },
            cancellationToken);

        return response;
    }
}