using GlobalGo.Application.Interfaces;
using GlobalGo.Domain.Entities;

namespace GlobalGo.Infrastructure.Repositories;

public sealed class InMemoryCreditEvaluationRepository : ICreditEvaluationRepository
{
    private readonly List<CreditEvaluation> _evaluations = [];
    private readonly object _lock = new();

    public Task AddAsync(CreditEvaluation evaluation, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _evaluations.Add(evaluation);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CreditEvaluation>> GetByDniAsync(string dni, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var result = _evaluations
                .Where(x => x.ApplicantDni == dni)
                .ToList();

            return Task.FromResult<IReadOnlyList<CreditEvaluation>>(result);
        }
    }

    public Task<IReadOnlyList<CreditEvaluation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<CreditEvaluation>>(_evaluations.ToList());
        }
    }

    public Task<CreditEvaluation?> GetRecentSameRequestAsync(
        string dni,
        decimal amountRequested,
        TimeSpan window,
        CancellationToken cancellationToken = default)
    {
        var threshold = DateTime.UtcNow.Subtract(window);

        lock (_lock)
        {
            var match = _evaluations
                .Where(x => x.ApplicantDni == dni
                    && x.AmountRequested == amountRequested
                    && x.CreatedAtUtc >= threshold)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefault();

            return Task.FromResult(match);
        }
    }
}