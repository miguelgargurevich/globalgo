using GlobalGo.Domain.Entities;

namespace GlobalGo.Application.Specifications;

public sealed record CreditDecisionContext(
    decimal AmountRequested,
    decimal MonthlyIncome,
    EquifaxReport Equifax,
    ReniecReport Reniec,
    SbsReport Sbs)
{
    public decimal InstallmentPressure => MonthlyIncome == 0 ? decimal.MaxValue : AmountRequested / MonthlyIncome;
}

public static class CreditDecisionSpecifications
{
    public static readonly ISpecification<CreditDecisionContext> IdentityRejected =
        new PredicateSpecification<CreditDecisionContext>(x => !x.Reniec.IdentityValid || x.Reniec.IsDeceased);

    public static readonly ISpecification<CreditDecisionContext> HighRiskRejected =
        new PredicateSpecification<CreditDecisionContext>(x =>
            x.Sbs.HasJudicialCollection
            || x.Sbs.DebtToIncomeRatio > 0.70m
            || x.Equifax.Score < 500);

    public static readonly ISpecification<CreditDecisionContext> AutoApproved =
        new PredicateSpecification<CreditDecisionContext>(x =>
            x.Equifax.Score >= 700
            && !x.Equifax.HasDelinquency
            && x.Sbs.DebtToIncomeRatio <= 0.40m
            && x.Sbs.ActiveCredits <= 1
            && x.InstallmentPressure <= 0.80m);
}