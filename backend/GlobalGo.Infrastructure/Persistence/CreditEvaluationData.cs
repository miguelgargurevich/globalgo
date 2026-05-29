using GlobalGo.Domain.Enums;

namespace GlobalGo.Infrastructure.Persistence;

public sealed class CreditEvaluationData
{
    public Guid Id { get; set; }
    public string ApplicantDni { get; set; } = string.Empty;
    public string ApplicantFullName { get; set; } = string.Empty;
    public decimal AmountRequested { get; set; }
    public decimal MonthlyIncome { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public CreditDecisionStatus Decision { get; set; }
    public string Justification { get; set; } = string.Empty;
    public bool ReusedRecentEvaluation { get; set; }

    public int EquifaxScore { get; set; }
    public bool EquifaxHasDelinquency { get; set; }
    public int EquifaxLatePaymentsLast12Months { get; set; }

    public bool ReniecIdentityValid { get; set; }
    public bool ReniecIsDeceased { get; set; }

    public decimal SbsDebtToIncomeRatio { get; set; }
    public int SbsActiveCredits { get; set; }
    public bool SbsHasJudicialCollection { get; set; }
}