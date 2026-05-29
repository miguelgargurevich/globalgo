using GlobalGo.Application.DTOs;
using GlobalGo.Application.Interfaces;
using GlobalGo.Domain.Entities;
using GlobalGo.Domain.Enums;

namespace GlobalGo.Application.Services;

public sealed class CreditEvaluationService : ICreditEvaluationService
{
    private static readonly TimeSpan RecentDuplicateWindow = TimeSpan.FromHours(24);

    private readonly ICreditEvaluationRepository _repository;
    private readonly IReadOnlyCollection<ICreditSourceProvider> _sourceProviders;

    public CreditEvaluationService(
        ICreditEvaluationRepository repository,
        IEnumerable<ICreditSourceProvider> sourceProviders)
    {
        _repository = repository;
        _sourceProviders = sourceProviders.ToList();
    }

    public async Task<CreditEvaluationResponse> EvaluateAsync(EvaluateCreditRequest request, CancellationToken cancellationToken = default)
    {
        var recentEvaluation = await _repository.GetRecentSameRequestAsync(
            request.Dni,
            request.AmountRequested,
            RecentDuplicateWindow,
            cancellationToken);

        if (recentEvaluation is not null)
        {
            return ToResponse(recentEvaluation with { ReusedRecentEvaluation = true });
        }

        var sourceContext = new CreditSourceContext(request.Dni, request.MonthlyIncome);
        var sourceResults = await QuerySourcesAsync(sourceContext, cancellationToken);

        var equifaxReport = GetRequiredReport<EquifaxReport>(sourceResults, CreditSourceCodes.Equifax);
        var reniecReport = GetRequiredReport<ReniecReport>(sourceResults, CreditSourceCodes.Reniec);
        var sbsReport = GetRequiredReport<SbsReport>(sourceResults, CreditSourceCodes.Sbs);

        var decisionResult = BuildDecision(
            request.AmountRequested,
            request.MonthlyIncome,
            equifaxReport,
            reniecReport,
            sbsReport);

        var evaluation = new CreditEvaluation(
            Guid.NewGuid(),
            request.Dni, 
            request.FullName,
            request.AmountRequested,
            request.MonthlyIncome,
            DateTime.UtcNow,
            decisionResult.Decision,
            decisionResult.Justification,
            equifaxReport,
            reniecReport,
            sbsReport,
            ReusedRecentEvaluation: false);

        await _repository.AddAsync(evaluation, cancellationToken);

        return ToResponse(evaluation);
    }

    public async Task<IReadOnlyList<CustomerHistoryItemResponse>> GetCustomerHistoryAsync(string dni, CancellationToken cancellationToken = default)
    {
        var history = await _repository.GetByDniAsync(dni, cancellationToken);

        return history
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new CustomerHistoryItemResponse(
                x.Id,
                x.CreatedAtUtc,
                x.AmountRequested,
                x.Decision,
                x.Justification,
                x.ReusedRecentEvaluation))
            .ToList();
    }

    public async Task<IReadOnlyList<GeneralHistoryItemResponse>> GetGeneralHistoryAsync(
        GeneralHistoryFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var history = await _repository.GetAllAsync(cancellationToken);

        var query = history.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter.Dni))
        {
            query = query.Where(x => x.ApplicantDni.Contains(filter.Dni.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (filter.Decision is not null)
        {
            query = query.Where(x => x.Decision == filter.Decision);
        }

        if (filter.FromUtc is not null)
        {
            query = query.Where(x => x.CreatedAtUtc >= filter.FromUtc.Value);
        }

        if (filter.ToUtc is not null)
        {
            query = query.Where(x => x.CreatedAtUtc <= filter.ToUtc.Value);
        }

        if (filter.MinAmount is not null)
        {
            query = query.Where(x => x.AmountRequested >= filter.MinAmount.Value);
        }

        if (filter.MaxAmount is not null)
        {
            query = query.Where(x => x.AmountRequested <= filter.MaxAmount.Value);
        }

        return query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new GeneralHistoryItemResponse(
                x.Id,
                x.CreatedAtUtc,
                x.ApplicantDni,
                x.ApplicantFullName,
                x.AmountRequested,
                x.MonthlyIncome,
                x.Decision,
                x.Justification,
                x.ReusedRecentEvaluation))
            .ToList();
    }

    public async Task<PortfolioRiskReportResponse> GetPortfolioRiskReportAsync(CancellationToken cancellationToken = default)
    {
        var evaluations = await _repository.GetAllAsync(cancellationToken);
        var totalEvaluations = evaluations.Count;

        if (totalEvaluations == 0)
        {
            return new PortfolioRiskReportResponse(
                TotalEvaluations: 0,
                TotalAmountEvaluated: 0,
                AverageAmount: 0,
                AverageDebtToIncomeRatio: 0,
                RejectionRate: 0,
                ObservationRate: 0,
                DistributionByState: []);
        }

        var distribution = evaluations
            .GroupBy(x => x.Decision)
            .Select(group => new PortfolioStateSummary(
                group.Key,
                group.Count(),
                group.Sum(x => x.AmountRequested),
                Math.Round(group.Count() * 100m / totalEvaluations, 2)))
            .OrderBy(x => x.Status)
            .ToList();

        var rejectedCount = evaluations.Count(x => x.Decision == CreditDecisionStatus.Rejected);
        var observedCount = evaluations.Count(x => x.Decision == CreditDecisionStatus.Observed);

        return new PortfolioRiskReportResponse(
            TotalEvaluations: totalEvaluations,
            TotalAmountEvaluated: evaluations.Sum(x => x.AmountRequested),
            AverageAmount: Math.Round(evaluations.Average(x => x.AmountRequested), 2),
            AverageDebtToIncomeRatio: Math.Round(evaluations.Average(x => x.SbsReport.DebtToIncomeRatio), 2),
            RejectionRate: Math.Round(rejectedCount * 100m / totalEvaluations, 2),
            ObservationRate: Math.Round(observedCount * 100m / totalEvaluations, 2),
            DistributionByState: distribution);
    }

    private static (CreditDecisionStatus Decision, string Justification) BuildDecision(
        decimal amountRequested,
        decimal monthlyIncome,
        EquifaxReport equifax,
        ReniecReport reniec,
        SbsReport sbs)
    {
        if (!reniec.IdentityValid || reniec.IsDeceased)
        {
            return (CreditDecisionStatus.Rejected, "RENIEC indica identidad no válida o persona fallecida.");
        }

        var installmentPressure = monthlyIncome == 0 ? decimal.MaxValue : amountRequested / monthlyIncome;

        if (sbs.HasJudicialCollection || sbs.DebtToIncomeRatio > 0.70m || equifax.Score < 500)
        {
            return (CreditDecisionStatus.Rejected, "Riesgo alto por score bajo, sobreendeudamiento o cobranza judicial.");
        }

        if (equifax.Score >= 700
            && !equifax.HasDelinquency
            && sbs.DebtToIncomeRatio <= 0.40m
            && sbs.ActiveCredits <= 1
            && installmentPressure <= 0.80m)
        {
            return (CreditDecisionStatus.Approved, "Perfil de riesgo saludable para aprobación automática.");
        }

        return (CreditDecisionStatus.Observed, "Caso intermedio: requiere observación manual por riesgo moderado.");
    }

    private async Task<IReadOnlyDictionary<string, object>> QuerySourcesAsync(
        CreditSourceContext context,
        CancellationToken cancellationToken)
    {
        var tasks = _sourceProviders
            .Select(async provider => new
            {
                provider.SourceCode,
                Report = await provider.GetReportAsync(context, cancellationToken)
            })
            .ToArray();

        var results = await Task.WhenAll(tasks);

        return results.ToDictionary(
            x => x.SourceCode,
            x => x.Report,
            StringComparer.OrdinalIgnoreCase);
    }

    private static TReport GetRequiredReport<TReport>(IReadOnlyDictionary<string, object> sourceResults, string sourceCode)
    {
        if (!sourceResults.TryGetValue(sourceCode, out var rawReport))
        {
            throw new InvalidOperationException($"No se encontró proveedor configurado para la fuente '{sourceCode}'.");
        }

        if (rawReport is not TReport typedReport)
        {
            throw new InvalidOperationException($"La fuente '{sourceCode}' devolvió un tipo inesperado: {rawReport.GetType().Name}.");
        }

        return typedReport;
    }

    private static CreditEvaluationResponse ToResponse(CreditEvaluation evaluation)
        => new(
            evaluation.Id,
            evaluation.ApplicantDni,
            evaluation.ApplicantFullName,
            evaluation.AmountRequested,
            evaluation.MonthlyIncome,
            evaluation.CreatedAtUtc,
            evaluation.Decision,
            evaluation.Justification,
            evaluation.EquifaxReport,
            evaluation.ReniecReport,
            evaluation.SbsReport,
            evaluation.ReusedRecentEvaluation);
}