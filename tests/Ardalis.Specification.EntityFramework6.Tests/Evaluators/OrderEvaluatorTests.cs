namespace Tests.Evaluators;

[Collection("SharedCollection")]
public class OrderEvaluatorTests(TestFactory factory) : IntegrationTest(factory)
{
    private static readonly Ardalis.Specification.EntityFramework6.OrderEvaluator _evaluator =
        Ardalis.Specification.EntityFramework6.OrderEvaluator.Instance;

    [Fact]
    public void QueriesMatch_GivenOrder()
    {
        var spec = new Specification<Company>();
        spec.Query
            .OrderBy(x => x.Id);

        var actual = _evaluator.GetQuery(DbContext.Companies, spec);
        var actualSql = GetQueryString(DbContext, actual);

        var expected = DbContext.Companies
            .OrderBy(x => x.Id);
        var expectedSql = GetQueryString(DbContext, expected);

        actualSql.Should().Be(expectedSql);
    }

    [Fact]
    public void QueriesMatch_GivenOrderChain()
    {
        var spec = new Specification<Company>();
        spec.Query
            .OrderBy(x => x.Id)
            .ThenBy(x => x.Name);

        var actual = _evaluator.GetQuery(DbContext.Companies, spec);
        var actualSql = GetQueryString(DbContext, actual);

        var expected = DbContext.Companies
            .OrderBy(x => x.Id)
            .ThenBy(x => x.Name);
        var expectedSql = GetQueryString(DbContext, expected);

        actualSql.Should().Be(expectedSql);
    }

    [Fact]
    public void QueriesMatch_GivenOrderByDescending()
    {
        var spec = new Specification<Company>();
        spec.Query
            .OrderByDescending(x => x.Id);

        var actual = _evaluator.GetQuery(DbContext.Companies, spec);
        var actualSql = GetQueryString(DbContext, actual);

        var expected = DbContext.Companies
            .OrderByDescending(x => x.Id);
        var expectedSql = GetQueryString(DbContext, expected);

        actualSql.Should().Be(expectedSql);
    }

    [Fact]
    public void QueriesMatch_GivenOrderByThenByDescending()
    {
        var spec = new Specification<Company>();
        spec.Query
            .OrderBy(x => x.Name)
            .ThenByDescending(x => x.Id);

        var actual = _evaluator.GetQuery(DbContext.Companies, spec);
        var actualSql = GetQueryString(DbContext, actual);

        var expected = DbContext.Companies
            .OrderBy(x => x.Name)
            .ThenByDescending(x => x.Id);
        var expectedSql = GetQueryString(DbContext, expected);

        actualSql.Should().Be(expectedSql);
    }

    [Fact]
    public void QueriesMatch_GivenFourKeyChainWithNavigationKey()
    {
        var spec = new Specification<Store>();
        spec.Query
            .OrderBy(x => x.Company.Name)
            .ThenByDescending(x => x.Id)
            .ThenBy(x => x.City)
            .ThenByDescending(x => x.CompanyId);

        var actual = _evaluator.GetQuery(DbContext.Stores, spec);
        var actualSql = GetQueryString(DbContext, actual);

        var expected = DbContext.Stores
            .OrderBy(x => x.Company.Name)
            .ThenByDescending(x => x.Id)
            .ThenBy(x => x.City)
            .ThenByDescending(x => x.CompanyId);
        var expectedSql = GetQueryString(DbContext, expected);

        actualSql.Should().Be(expectedSql);
    }

    [Fact]
    public void ThrowsDuplicateOrderChainException_GivenTwoOrderChains()
    {
        var spec = new Specification<Company>();
        spec.Query
            .OrderBy(x => x.Id)
            .OrderByDescending(x => x.Name);

        var getQuery = () => _evaluator.GetQuery(DbContext.Companies, spec);
        var evaluate = () => _evaluator.Evaluate(new List<Company>(), spec);

        getQuery.Should().Throw<DuplicateOrderChainException>();
        evaluate.Should().Throw<DuplicateOrderChainException>();
    }

    [Fact]
    public void OrdersItemsInMemory_GivenOrderByDescendingThenBy()
    {
        List<Company> input =
        [
            new() { Id = 1, Name = "b", CountryId = 1 },
            new() { Id = 2, Name = "c", CountryId = 2 },
            new() { Id = 3, Name = "a", CountryId = 1 },
            new() { Id = 4, Name = "d", CountryId = 2 },
        ];

        var spec = new Specification<Company>();
        spec.Query
            .OrderByDescending(x => x.CountryId)
            .ThenBy(x => x.Name);

        var actual = _evaluator.Evaluate(input, spec);

        actual.Select(x => x.Id).Should().Equal(2, 4, 3, 1);
    }
}
