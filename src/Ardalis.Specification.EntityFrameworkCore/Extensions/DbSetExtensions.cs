namespace Ardalis.Specification.EntityFrameworkCore;

public static class DbSetExtensions
{
    public static async Task<List<TSource>> ToListAsync<TSource>(
      this DbSet<TSource> source,
      ISpecification<TSource> specification,
      CancellationToken cancellationToken = default)
      where TSource : class
    {
        var result = await SpecificationEvaluator.Default.GetQuery(source, specification).ToListAsync(cancellationToken);

        return specification.PostProcessingAction is null
            ? result
            : specification.PostProcessingAction(result).AsList();
    }

    public static async Task<List<TResult>> ToListAsync<TSource, TResult>(
      this DbSet<TSource> source,
      ISpecification<TSource, TResult> specification,
      CancellationToken cancellationToken = default)
      where TSource : class
    {
        var result = await SpecificationEvaluator.Default.GetQuery(source, specification).ToListAsync(cancellationToken);

        return specification.PostProcessingAction is null
            ? result
            : specification.PostProcessingAction(result).AsList();
    }

    public static async Task<IEnumerable<TSource>> ToEnumerableAsync<TSource>(
      this DbSet<TSource> source,
      ISpecification<TSource> specification,
      CancellationToken cancellationToken = default)
      where TSource : class
    {
        var result = await SpecificationEvaluator.Default.GetQuery(source, specification).ToListAsync(cancellationToken);

        return specification.PostProcessingAction is null
            ? result
            : specification.PostProcessingAction(result);
    }

    public static async Task<IEnumerable<TResult>> ToEnumerableAsync<TSource, TResult>(
      this DbSet<TSource> source,
      ISpecification<TSource, TResult> specification,
      CancellationToken cancellationToken = default)
      where TSource : class
    {
        var result = await SpecificationEvaluator.Default.GetQuery(source, specification).ToListAsync(cancellationToken);

        return specification.PostProcessingAction is null
            ? result
            : specification.PostProcessingAction(result);
    }

    public static async Task<TSource?> FirstOrDefaultAsync<TSource>(
      this DbSet<TSource> source,
      ISpecification<TSource> specification,
      CancellationToken cancellationToken = default)
      where TSource : class
    {
        return await SpecificationEvaluator.Default.GetQuery(source, specification).FirstOrDefaultAsync(cancellationToken);
    }

    public static async Task<TResult?> FirstOrDefaultAsync<TSource, TResult>(
      this DbSet<TSource> source,
      ISpecification<TSource, TResult> specification,
      CancellationToken cancellationToken = default)
      where TSource : class
    {
        return await SpecificationEvaluator.Default.GetQuery(source, specification).FirstOrDefaultAsync(cancellationToken);
    }

    public static async Task<TSource?> SingleOrDefaultAsync<TSource>(
      this DbSet<TSource> source,
      ISingleResultSpecification<TSource> specification,
      CancellationToken cancellationToken = default)
      where TSource : class
    {
        return await SpecificationEvaluator.Default.GetQuery(source, specification).SingleOrDefaultAsync(cancellationToken);
    }

    public static async Task<TResult?> SingleOrDefaultAsync<TSource, TResult>(
      this DbSet<TSource> source,
      ISingleResultSpecification<TSource, TResult> specification,
      CancellationToken cancellationToken = default)
      where TSource : class
    {
        return await SpecificationEvaluator.Default.GetQuery(source, specification).SingleOrDefaultAsync(cancellationToken);
    }

    public static async Task<int> CountAsync<TSource>(
      this DbSet<TSource> source,
      ISpecification<TSource> specification,
      CancellationToken cancellationToken = default)
      where TSource : class
    {
        return await SpecificationEvaluator.Default.GetQuery(source, specification, evaluateCriteriaOnly: true).CountAsync(cancellationToken);
    }

    public static async Task<bool> AnyAsync<TSource>(
      this DbSet<TSource> source,
      ISpecification<TSource> specification,
      CancellationToken cancellationToken = default)
      where TSource : class
    {
        return await SpecificationEvaluator.Default.GetQuery(source, specification, evaluateCriteriaOnly: true).AnyAsync(cancellationToken);
    }

    public static IAsyncEnumerable<TSource> AsAsyncEnumerable<TSource>(
      this DbSet<TSource> source,
      ISpecification<TSource> specification)
      where TSource : class
    {
        return SpecificationEvaluator.Default.GetQuery(source, specification).AsAsyncEnumerable();
    }

    public static IQueryable<TSource> WithSpecification<TSource>(
      this IQueryable<TSource> source,
      ISpecification<TSource> specification,
      ISpecificationEvaluator? evaluator = null)
      where TSource : class
    {
        evaluator ??= SpecificationEvaluator.Default;
        return evaluator.GetQuery(source, specification);
    }

    public static IQueryable<TResult> WithSpecification<TSource, TResult>(
      this IQueryable<TSource> source,
      ISpecification<TSource, TResult> specification,
      ISpecificationEvaluator? evaluator = null)
      where TSource : class
    {
        evaluator ??= SpecificationEvaluator.Default;
        return evaluator.GetQuery(source, specification);
    }
}
