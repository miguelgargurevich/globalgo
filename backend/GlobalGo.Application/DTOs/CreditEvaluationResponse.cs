using GlobalGo.Domain.Entities;
using GlobalGo.Domain.Enums;

namespace GlobalGo.Application.DTOs;

public sealed record CreditEvaluationResponse(
    Guid EvaluationId,
    string Dni,
    string FullName,
    decimal AmountRequested,
    decimal MonthlyIncome,
    DateTime CreatedAtUtc,
    CreditDecisionStatus Decision,
    string Justification,
    EquifaxReport EquifaxReport,
    ReniecReport ReniecReport,
    SbsReport SbsReport,
    bool ReusedRecentEvaluation);

public sealed record CustomerHistoryItemResponse(
    Guid EvaluationId,
    DateTime CreatedAtUtc,
    decimal AmountRequested,
    CreditDecisionStatus Decision,
    string Justification,
    bool ReusedRecentEvaluation,
    EquifaxReport EquifaxReport,
    ReniecReport ReniecReport,
    SbsReport SbsReport);

public sealed record CustomerProfileResponse(
    string Dni,
    bool Exists,
    string? FullName,
    DateTime? LastEvaluationAtUtc);

public sealed record GeneralHistoryFilterRequest(
    string? Dni,
    CreditDecisionStatus? Decision,
    DateTime? FromUtc,
    DateTime? ToUtc,
    decimal? MinAmount,
    decimal? MaxAmount);

public sealed record GeneralHistoryItemResponse(
    Guid EvaluationId,
    DateTime CreatedAtUtc,
    string Dni,
    string FullName,
    decimal AmountRequested,
    decimal MonthlyIncome,
    CreditDecisionStatus Decision,
    string Justification,
    bool ReusedRecentEvaluation);

public sealed record PortfolioStateSummary(
    CreditDecisionStatus Status,
    int Count,
    decimal TotalAmount,
    decimal Percentage);

public sealed record PortfolioRiskReportResponse(
    int TotalEvaluations,
    decimal TotalAmountEvaluated,
    decimal AverageAmount,
    decimal AverageDebtToIncomeRatio,
    decimal RejectionRate,
    decimal ObservationRate,
    IReadOnlyList<PortfolioStateSummary> DistributionByState);