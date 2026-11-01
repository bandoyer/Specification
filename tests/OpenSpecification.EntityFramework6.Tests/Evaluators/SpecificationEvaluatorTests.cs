namespace Tests.Evaluators;

public class SpecificationEvaluatorTests
{
    private static readonly SpecificationEvaluator _evaluator = SpecificationEvaluator.Default;

    // GetQuery checks the specification before it reads the query, so these tests need no database.
    private static readonly IQueryable<Country> _countries = new List<Country>().AsQueryable();

    [Fact]
    public void ThrowsArgumentNullException_GivenNullSpec()
    {
        var sut = () => _evaluator.GetQuery(_countries, (Specification<Country>)null!);

        sut.Should().Throw<ArgumentNullException>().WithParameterName("specification");
    }

    [Fact]
    public void ThrowsArgumentNullException_GivenNullSpecificationWithSelector()
    {
        var sut = () => _evaluator.GetQuery(_countries, (Specification<Country, string>)null!);

        sut.Should().Throw<ArgumentNullException>().WithParameterName("specification");
    }

    [Fact]
    public void Constructor_SetsDefaultEvaluators()
    {
        var result = new SpecificationEvaluatorDerived().EvaluatorList;

        result.Should().HaveCount(7);
        result[0].Should().BeOfType<WhereEvaluator>();
        result[1].Should().BeOfType<SearchEvaluator>();
        result[2].Should().BeOfType<IncludeStringEvaluator>();
        result[3].Should().BeOfType<IncludeEvaluator>();
        result[4].Should().BeOfType<OpenSpecification.EntityFramework6.OrderEvaluator>();
        result[5].Should().BeOfType<PaginationEvaluator>();
        result[6].Should().BeOfType<AsNoTrackingEvaluator>();
    }

    // Reads the protected list. The EF Core tests use UnsafeAccessor, which net472 doesn't have.
    private class SpecificationEvaluatorDerived : SpecificationEvaluator
    {
        public List<IEvaluator> EvaluatorList => Evaluators;
    }
}
