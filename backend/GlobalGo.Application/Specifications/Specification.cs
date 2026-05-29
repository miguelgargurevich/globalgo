namespace GlobalGo.Application.Specifications;

public interface ISpecification<T>
{
    bool IsSatisfiedBy(T candidate);
}

public sealed class PredicateSpecification<T> : ISpecification<T>
{
    private readonly Func<T, bool> _predicate;

    public PredicateSpecification(Func<T, bool> predicate)
    {
        _predicate = predicate;
    }

    public bool IsSatisfiedBy(T candidate) => _predicate(candidate);
}

public static class Specification
{
    public static ISpecification<T> All<T>() => new PredicateSpecification<T>(_ => true);
}

public static class SpecificationExtensions
{
    public static ISpecification<T> And<T>(this ISpecification<T> left, ISpecification<T> right)
        => new PredicateSpecification<T>(candidate => left.IsSatisfiedBy(candidate) && right.IsSatisfiedBy(candidate));

    public static ISpecification<T> Or<T>(this ISpecification<T> left, ISpecification<T> right)
        => new PredicateSpecification<T>(candidate => left.IsSatisfiedBy(candidate) || right.IsSatisfiedBy(candidate));

    public static ISpecification<T> Not<T>(this ISpecification<T> source)
        => new PredicateSpecification<T>(candidate => !source.IsSatisfiedBy(candidate));
}