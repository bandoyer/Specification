// A specification of the caller's own that implements ISpecification<Company> with every member except Query. Since
// #8 it builds; before #8 it fails with CS0535 for the missing Query. features/query-builder.md copies it into a
// consumer. Nothing calls it.
using Ardalis.Specification;

class SpecificationWithoutQuery : ISpecification<Company>
{
    public Dictionary<string, object> Items => throw new NotImplementedException();
    public IEnumerable<WhereExpressionInfo<Company>> WhereExpressions => throw new NotImplementedException();
    public IEnumerable<OrderExpressionInfo<Company>> OrderExpressions => throw new NotImplementedException();
    public IEnumerable<IncludeExpressionInfo> IncludeExpressions => throw new NotImplementedException();
    public IEnumerable<string> IncludeStrings => throw new NotImplementedException();
    public IEnumerable<SearchExpressionInfo<Company>> SearchCriterias => throw new NotImplementedException();
    public int Take => throw new NotImplementedException();
    public int Skip => throw new NotImplementedException();
    public Func<IEnumerable<Company>, IEnumerable<Company>>? PostProcessingAction => throw new NotImplementedException();
    public IEnumerable<string> QueryTags => throw new NotImplementedException();
    public bool CacheEnabled => throw new NotImplementedException();
    public string? CacheKey => throw new NotImplementedException();
    public bool AsTracking => throw new NotImplementedException();
    public bool AsNoTracking => throw new NotImplementedException();
    public bool AsSplitQuery => throw new NotImplementedException();
    public bool AsNoTrackingWithIdentityResolution => throw new NotImplementedException();
    public bool IgnoreQueryFilters => throw new NotImplementedException();
    public bool IgnoreAutoIncludes => throw new NotImplementedException();
    public IEnumerable<Company> Evaluate(IEnumerable<Company> entities) => throw new NotImplementedException();
    public bool IsSatisfiedBy(Company entity) => throw new NotImplementedException();
}
