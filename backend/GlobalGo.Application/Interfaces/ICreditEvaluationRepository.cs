using GlobalGo.Domain.Entities;

namespace GlobalGo.Application.Interfaces;

public interface ICreditEvaluationRepository
{
    Task AddAsync(CreditEvaluation evaluation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CreditEvaluation>> GetByDniAsync(string dni, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CreditEvaluation>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CreditEvaluation?> GetRecentSameRequestAsync(string dni, decimal amountRequested, TimeSpan window, CancellationToken cancellationToken = default);
}

public sealed record CreditSourceContext(string Dni, decimal MonthlyIncome);

public interface ICreditSourceProvider
{
    string SourceCode { get; }
    Task<object> GetReportAsync(CreditSourceContext context, CancellationToken cancellationToken = default);
}

public static class CreditSourceCodes
{
    public const string Equifax = "equifax";
    public const string Reniec = "reniec";
    public const string Sbs = "sbs";
}