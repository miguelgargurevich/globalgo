using GlobalGo.Application.Interfaces;
using GlobalGo.Domain.Entities;
using GlobalGo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GlobalGo.Infrastructure.Repositories;

public sealed class PostgresCreditEvaluationRepository : ICreditEvaluationRepository
{
    private readonly CreditEvaluationDbContext _dbContext;

    public PostgresCreditEvaluationRepository(CreditEvaluationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(CreditEvaluation evaluation, CancellationToken cancellationToken = default)
    {
        _dbContext.CreditEvaluations.Add(ToData(evaluation));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CreditEvaluation>> GetByDniAsync(string dni, CancellationToken cancellationToken = default)
        => await _dbContext.CreditEvaluations
            .AsNoTracking()
            .Where(x => x.ApplicantDni == dni)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(ToDomainExpression())
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CreditEvaluation>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbContext.CreditEvaluations
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(ToDomainExpression())
            .ToListAsync(cancellationToken);

    public async Task<CreditEvaluation?> GetRecentSameRequestAsync(
        string dni,
        decimal amountRequested,
        TimeSpan window,
        CancellationToken cancellationToken = default)
    {
        var threshold = DateTime.UtcNow.Subtract(window);

        return await _dbContext.CreditEvaluations
            .AsNoTracking()
            .Where(x => x.ApplicantDni == dni
                && x.AmountRequested == amountRequested
                && x.CreatedAtUtc >= threshold)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(ToDomainExpression())
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static CreditEvaluationData ToData(CreditEvaluation evaluation)
        => new()
        {
            Id = evaluation.Id,
            ApplicantDni = evaluation.ApplicantDni,
            ApplicantFullName = evaluation.ApplicantFullName,
            AmountRequested = evaluation.AmountRequested,
            MonthlyIncome = evaluation.MonthlyIncome,
            CreatedAtUtc = evaluation.CreatedAtUtc,
            Decision = evaluation.Decision,
            Justification = evaluation.Justification,
            ReusedRecentEvaluation = evaluation.ReusedRecentEvaluation,

            EquifaxScore = evaluation.EquifaxReport.Score,
            EquifaxHasDelinquency = evaluation.EquifaxReport.HasDelinquency,
            EquifaxLatePaymentsLast12Months = evaluation.EquifaxReport.LatePaymentsLast12Months,

            ReniecIdentityValid = evaluation.ReniecReport.IdentityValid,
            ReniecIsDeceased = evaluation.ReniecReport.IsDeceased,

            SbsDebtToIncomeRatio = evaluation.SbsReport.DebtToIncomeRatio,
            SbsActiveCredits = evaluation.SbsReport.ActiveCredits,
            SbsHasJudicialCollection = evaluation.SbsReport.HasJudicialCollection,
        };

    private static System.Linq.Expressions.Expression<Func<CreditEvaluationData, CreditEvaluation>> ToDomainExpression()
        => data => new CreditEvaluation(
            data.Id,
            data.ApplicantDni,
            data.ApplicantFullName,
            data.AmountRequested,
            data.MonthlyIncome,
            data.CreatedAtUtc,
            data.Decision,
            data.Justification,
            new EquifaxReport(
                data.EquifaxScore,
                data.EquifaxHasDelinquency,
                data.EquifaxLatePaymentsLast12Months),
            new ReniecReport(
                data.ReniecIdentityValid,
                data.ReniecIsDeceased),
            new SbsReport(
                data.SbsDebtToIncomeRatio,
                data.SbsActiveCredits,
                data.SbsHasJudicialCollection),
            data.ReusedRecentEvaluation);
}