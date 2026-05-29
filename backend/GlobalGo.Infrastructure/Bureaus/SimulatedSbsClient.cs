using GlobalGo.Application.Interfaces;
using GlobalGo.Domain.Entities;

namespace GlobalGo.Infrastructure.Bureaus;

public sealed class SimulatedSbsClient : ICreditSourceProvider
{
    private readonly ISimulatedBureauApiGateway _apiGateway;

    public SimulatedSbsClient(ISimulatedBureauApiGateway apiGateway)
    {
        _apiGateway = apiGateway;
    }

    public string SourceCode => CreditSourceCodes.Sbs;

    public async Task<object> GetReportAsync(CreditSourceContext context, CancellationToken cancellationToken = default)
    {
        var response = await _apiGateway.RequestAsync(
            SourceCode,
            context,
            seed =>
            {
                var rawRatio = (seed % 91) / 100m;
                var debtToIncomeRatio = context.MonthlyIncome < 1000
                    ? Math.Min(0.95m, rawRatio + 0.10m)
                    : rawRatio;
                var activeCredits = seed % 6;
                var hasJudicialCollection = (seed % 31) == 0;
                return new SbsReport(debtToIncomeRatio, activeCredits, hasJudicialCollection);
            },
            cancellationToken);

        return response;
    }
}