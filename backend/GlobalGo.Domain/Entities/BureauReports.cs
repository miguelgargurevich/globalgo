namespace GlobalGo.Domain.Entities;

public sealed record EquifaxReport(
    int Score,
    bool HasDelinquency,
    int LatePaymentsLast12Months);

public sealed record ReniecReport(
    bool IdentityValid,
    bool IsDeceased);

public sealed record SbsReport(
    decimal DebtToIncomeRatio,
    int ActiveCredits,
    bool HasJudicialCollection);