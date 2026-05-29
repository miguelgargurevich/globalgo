using GlobalGo.Application.DTOs;
using GlobalGo.Domain.Entities;

namespace GlobalGo.Application.Specifications;

public static class CreditEvaluationHistorySpecifications
{
    public static ISpecification<CreditEvaluation> Build(GeneralHistoryFilterRequest filter)
    {
        var specification = Specification.All<CreditEvaluation>();

        if (!string.IsNullOrWhiteSpace(filter.Dni))
        {
            var dni = filter.Dni.Trim();
            specification = specification.And(new PredicateSpecification<CreditEvaluation>(
                x => x.ApplicantDni.Contains(dni, StringComparison.OrdinalIgnoreCase)));
        }

        if (filter.Decision is not null)
        {
            specification = specification.And(new PredicateSpecification<CreditEvaluation>(
                x => x.Decision == filter.Decision));
        }

        if (filter.FromUtc is not null)
        {
            specification = specification.And(new PredicateSpecification<CreditEvaluation>(
                x => x.CreatedAtUtc >= filter.FromUtc.Value));
        }

        if (filter.ToUtc is not null)
        {
            specification = specification.And(new PredicateSpecification<CreditEvaluation>(
                x => x.CreatedAtUtc <= filter.ToUtc.Value));
        }

        if (filter.MinAmount is not null)
        {
            specification = specification.And(new PredicateSpecification<CreditEvaluation>(
                x => x.AmountRequested >= filter.MinAmount.Value));
        }

        if (filter.MaxAmount is not null)
        {
            specification = specification.And(new PredicateSpecification<CreditEvaluation>(
                x => x.AmountRequested <= filter.MaxAmount.Value));
        }

        return specification;
    }
}