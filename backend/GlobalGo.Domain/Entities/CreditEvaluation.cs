using GlobalGo.Domain.Enums;

namespace GlobalGo.Domain.Entities;

public sealed record CreditEvaluation(
    Guid Id,
    string ApplicantDni,
    string ApplicantFullName,
    decimal AmountRequested,
    decimal MonthlyIncome,
    DateTime CreatedAtUtc,
    CreditDecisionStatus Decision,
    string Justification,
    EquifaxReport EquifaxReport,
    ReniecReport ReniecReport,
    SbsReport SbsReport,
    bool ReusedRecentEvaluation);