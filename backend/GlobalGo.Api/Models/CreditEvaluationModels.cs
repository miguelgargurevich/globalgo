using System.ComponentModel.DataAnnotations;
using GlobalGo.Application.DTOs;
using GlobalGo.Domain.Enums;

namespace GlobalGo.Api.Models;

public sealed record EvaluateCreditRequestModel
{
    [Required]
    [StringLength(8, MinimumLength = 8)]
    public string Dni { get; init; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string FullName { get; init; } = string.Empty;

    [Range(1, 100_000)]
    public decimal AmountRequested { get; init; }

    [Range(300, 100_000)]
    public decimal MonthlyIncome { get; init; }
}

public sealed record CreditEvaluationResultModel(
    Guid EvaluationId,
    string Dni,
    string FullName,
    decimal AmountRequested,
    decimal MonthlyIncome,
    DateTime CreatedAtUtc,
    CreditDecisionStatus Decision,
    string Justification,
    EquifaxReportModel Equifax,
    ReniecReportModel Reniec,
    SbsReportModel Sbs,
    bool ReusedRecentEvaluation);

public sealed record EquifaxReportModel(int Score, bool HasDelinquency, int LatePaymentsLast12Months);
public sealed record ReniecReportModel(bool IdentityValid, bool IsDeceased);
public sealed record SbsReportModel(decimal DebtToIncomeRatio, int ActiveCredits, bool HasJudicialCollection);

public sealed record CustomerHistoryItemModel(
    Guid EvaluationId,
    DateTime CreatedAtUtc,
    decimal AmountRequested,
    CreditDecisionStatus Decision,
    string Justification,
    bool ReusedRecentEvaluation,
    EquifaxReportModel Equifax,
    ReniecReportModel Reniec,
    SbsReportModel Sbs);

public sealed record CustomerProfileModel(
    string Dni,
    bool Exists,
    string? FullName,
    DateTime? LastEvaluationAtUtc);

public sealed record GeneralHistoryFilterModel
{
    public string? Dni { get; init; }
    public CreditDecisionStatus? Decision { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public decimal? MinAmount { get; init; }
    public decimal? MaxAmount { get; init; }
}

public sealed record GeneralHistoryItemModel(
    Guid EvaluationId,
    DateTime CreatedAtUtc,
    string Dni,
    string FullName,
    decimal AmountRequested,
    decimal MonthlyIncome,
    CreditDecisionStatus Decision,
    string Justification,
    bool ReusedRecentEvaluation);

public sealed record PortfolioStateSummaryModel(
    CreditDecisionStatus Status,
    int Count,
    decimal TotalAmount,
    decimal Percentage);

public sealed record PortfolioRiskReportModel(
    int TotalEvaluations,
    decimal TotalAmountEvaluated,
    decimal AverageAmount,
    decimal AverageDebtToIncomeRatio,
    decimal RejectionRate,
    decimal ObservationRate,
    IReadOnlyList<PortfolioStateSummaryModel> DistributionByState);

public static class CreditEvaluationMappings
{
    public static EvaluateCreditRequest ToApplication(this EvaluateCreditRequestModel model)
        => new()
        {
            Dni = model.Dni,
            FullName = model.FullName,
            AmountRequested = model.AmountRequested,
            MonthlyIncome = model.MonthlyIncome
        };

    public static CreditEvaluationResultModel ToApi(this CreditEvaluationResponse response)
        => new(
            response.EvaluationId,
            response.Dni,
            response.FullName,
            response.AmountRequested,
            response.MonthlyIncome,
            response.CreatedAtUtc,
            response.Decision,
            response.Justification,
            new EquifaxReportModel(
                response.EquifaxReport.Score,
                response.EquifaxReport.HasDelinquency,
                response.EquifaxReport.LatePaymentsLast12Months),
            new ReniecReportModel(
                response.ReniecReport.IdentityValid,
                response.ReniecReport.IsDeceased),
            new SbsReportModel(
                response.SbsReport.DebtToIncomeRatio,
                response.SbsReport.ActiveCredits,
                response.SbsReport.HasJudicialCollection),
            response.ReusedRecentEvaluation);

    public static CustomerHistoryItemModel ToApi(this CustomerHistoryItemResponse response)
        => new(
            response.EvaluationId,
            response.CreatedAtUtc,
            response.AmountRequested,
            response.Decision,
            response.Justification,
            response.ReusedRecentEvaluation,
            new EquifaxReportModel(
                response.EquifaxReport.Score,
                response.EquifaxReport.HasDelinquency,
                response.EquifaxReport.LatePaymentsLast12Months),
            new ReniecReportModel(
                response.ReniecReport.IdentityValid,
                response.ReniecReport.IsDeceased),
            new SbsReportModel(
                response.SbsReport.DebtToIncomeRatio,
                response.SbsReport.ActiveCredits,
                response.SbsReport.HasJudicialCollection));

    public static CustomerProfileModel ToApi(this CustomerProfileResponse response)
        => new(
            response.Dni,
            response.Exists,
            response.FullName,
            response.LastEvaluationAtUtc);

    public static GeneralHistoryFilterRequest ToApplication(this GeneralHistoryFilterModel model)
        => new(
            model.Dni,
            model.Decision,
            model.FromUtc,
            model.ToUtc,
            model.MinAmount,
            model.MaxAmount);

    public static GeneralHistoryItemModel ToApi(this GeneralHistoryItemResponse response)
        => new(
            response.EvaluationId,
            response.CreatedAtUtc,
            response.Dni,
            response.FullName,
            response.AmountRequested,
            response.MonthlyIncome,
            response.Decision,
            response.Justification,
            response.ReusedRecentEvaluation);

    public static PortfolioRiskReportModel ToApi(this PortfolioRiskReportResponse response)
        => new(
            response.TotalEvaluations,
            response.TotalAmountEvaluated,
            response.AverageAmount,
            response.AverageDebtToIncomeRatio,
            response.RejectionRate,
            response.ObservationRate,
            response.DistributionByState
                .Select(x => new PortfolioStateSummaryModel(x.Status, x.Count, x.TotalAmount, x.Percentage))
                .ToList());
}