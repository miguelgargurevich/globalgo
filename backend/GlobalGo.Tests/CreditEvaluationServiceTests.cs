using GlobalGo.Application.DTOs;
using GlobalGo.Application.Interfaces;
using GlobalGo.Application.Services;
using GlobalGo.Domain.Entities;
using GlobalGo.Domain.Enums;
using GlobalGo.Infrastructure.Repositories;

namespace GlobalGo.Tests;

public sealed class CreditEvaluationServiceTests
{
    [Fact]
    public async Task EvaluateAsync_ShouldApprove_WhenRiskProfileIsHealthy()
    {
        var service = BuildService(
            new StubSourceProvider(CreditSourceCodes.Equifax, new EquifaxReport(780, false, 0)),
            new StubSourceProvider(CreditSourceCodes.Reniec, new ReniecReport(true, false)),
            new StubSourceProvider(CreditSourceCodes.Sbs, new SbsReport(0.25m, 1, false)));

        var request = BuildRequest() with { AmountRequested = 2000 };
        var result = await service.EvaluateAsync(request);

        Assert.Equal(CreditDecisionStatus.Approved, result.Decision);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldReject_WhenReniecInvalid()
    {
        var service = BuildService(
            new StubSourceProvider(CreditSourceCodes.Equifax, new EquifaxReport(800, false, 0)),
            new StubSourceProvider(CreditSourceCodes.Reniec, new ReniecReport(false, false)),
            new StubSourceProvider(CreditSourceCodes.Sbs, new SbsReport(0.10m, 0, false)));

        var request = BuildRequest();
        var result = await service.EvaluateAsync(request);

        Assert.Equal(CreditDecisionStatus.Rejected, result.Decision);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldObserve_WhenProfileIsIntermediate()
    {
        var service = BuildService(
            new StubSourceProvider(CreditSourceCodes.Equifax, new EquifaxReport(650, false, 2)),
            new StubSourceProvider(CreditSourceCodes.Reniec, new ReniecReport(true, false)),
            new StubSourceProvider(CreditSourceCodes.Sbs, new SbsReport(0.45m, 2, false)));

        var request = BuildRequest();
        var result = await service.EvaluateAsync(request);

        Assert.Equal(CreditDecisionStatus.Observed, result.Decision);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldReuseRecentEvaluation_WhenSameCustomerAndAmountWithin24Hours()
    {
        var equifax = new StubSourceProvider(CreditSourceCodes.Equifax, new EquifaxReport(760, false, 0));
        var service = BuildService(
            equifax,
            new StubSourceProvider(CreditSourceCodes.Reniec, new ReniecReport(true, false)),
            new StubSourceProvider(CreditSourceCodes.Sbs, new SbsReport(0.30m, 1, false)));

        var request = BuildRequest();

        var first = await service.EvaluateAsync(request);
        var second = await service.EvaluateAsync(request);

        Assert.False(first.ReusedRecentEvaluation);
        Assert.True(second.ReusedRecentEvaluation);
        Assert.Equal(1, equifax.CallCount);
    }

    private static CreditEvaluationService BuildService(params ICreditSourceProvider[] providers)
    {
        var repository = new InMemoryCreditEvaluationRepository();

        return new CreditEvaluationService(
            repository,
            providers);
    }

    private static EvaluateCreditRequest BuildRequest()
        => new()
        {
            Dni = "12345678",
            FullName = "Persona de Prueba",
            AmountRequested = 5000,
            MonthlyIncome = 3000
        };

    private sealed class StubSourceProvider : ICreditSourceProvider
    {
        private readonly object _report;

        public string SourceCode { get; }
        public int CallCount { get; private set; }

        public StubSourceProvider(string sourceCode, object report)
        {
            SourceCode = sourceCode;
            _report = report;
        }

        public Task<object> GetReportAsync(CreditSourceContext context, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(_report);
        }
    }
}