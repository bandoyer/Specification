using System.Data.Entity;

namespace Tests.Evaluators;

[Collection("SharedCollection")]
public class IncludeEvaluatorTests(TestFactory factory) : IntegrationTest(factory)
{
    private static readonly IncludeEvaluator _evaluator = IncludeEvaluator.Instance;

    [Fact]
    public void QueriesMatch_GivenNoIncludeExpression()
    {
        var spec = new Specification<Company>();

        var actual = _evaluator.GetQuery(DbContext.Companies, spec);
        var actualSql = GetQueryString(DbContext, actual);

        var expected = DbContext.Companies.AsQueryable();
        var expectedSql = GetQueryString(DbContext, expected);

        actualSql.Should().Be(expectedSql);
    }

    [Fact]
    public void QueriesMatch_GivenNoIncludeExpression_WithAutoOneToOne()
    {
        var spec = new Specification<Store>();

        var actual = _evaluator.GetQuery(DbContext.Stores, spec);
        var actualSql = GetQueryString(DbContext, actual);

        var expected = DbContext.Stores.AsQueryable();
        var expectedSql = GetQueryString(DbContext, expected);

        actualSql.Should().Be(expectedSql);
    }

    [Fact]
    public void QueriesMatch_GivenSingleIncludeExpression_WithReferenceNavigation()
    {
        var spec = new Specification<Company>();
        spec.Query
            .Include(x => x.Country);

        var actual = _evaluator.GetQuery(DbContext.Companies, spec);
        var actualSql = GetQueryString(DbContext, actual);

        var expected = DbContext.Companies
            .Include(x => x.Country);
        var expectedSql = GetQueryString(DbContext, expected);

        actualSql.Should().Be(expectedSql);
    }

    [Fact]
    public void QueriesMatch_GivenSingleIncludeExpression_WithCollectionNavigation()
    {
        var spec = new Specification<Company>();
        spec.Query
            .Include(x => x.Stores);

        var actual = _evaluator.GetQuery(DbContext.Companies, spec);
        var actualSql = GetQueryString(DbContext, actual);

        var expected = DbContext.Companies
            .Include(x => x.Stores);
        var expectedSql = GetQueryString(DbContext, expected);

        actualSql.Should().Be(expectedSql);
    }

    [Fact]
    public void QueriesMatch_GivenMultipleIncludeExpression()
    {
        var spec = new Specification<Company>();
        spec.Query
            .Include(x => x.Country)
            .Include(x => x.Stores);

        var actual = _evaluator.GetQuery(DbContext.Companies, spec);
        var actualSql = GetQueryString(DbContext, actual);

        var expected = DbContext.Companies
            .Include(x => x.Country)
            .Include(x => x.Stores);
        var expectedSql = GetQueryString(DbContext, expected);

        actualSql.Should().Be(expectedSql);
    }

    [Fact]
    public void DoesNothing_GivenIncludeString()
    {
        var spec = new Specification<Company>();
        spec.Query
            .Include(nameof(Company.Country));

        var actual = _evaluator.GetQuery(DbContext.Companies, spec);
        var actualSql = GetQueryString(DbContext, actual);

        // IncludeStringEvaluator applies include strings.
        var expected = DbContext.Companies.AsQueryable();
        var expectedSql = GetQueryString(DbContext, expected);

        actualSql.Should().Be(expectedSql);
    }


    [Fact]
    public void QueriesMatch_GivenThenIncludeExpression()
    {
        var spec = new Specification<Store>();
        spec.Query
            .Include(x => x.Products)
                .ThenInclude(x => x.Images)
            .Include(x => x.Company)
                .ThenInclude(x => x.Country);

        var actual = _evaluator.GetQuery(DbContext.Stores, spec);
        var actualSql = GetQueryString(DbContext, actual);

        // EF6 doesn't support ThenInclude, it uses string-based includes
        var expected = DbContext.Stores
            .Include($"{nameof(Store.Products)}.{nameof(Product.Images)}")
            .Include($"{nameof(Store.Company)}.{nameof(Company.Country)}");
        var expectedSql = GetQueryString(DbContext, expected);

        actualSql.Should().Be(expectedSql);
    }

    [Fact]
    public void QueriesMatch_GivenSingleThenIncludeExpression()
    {
        var spec = new Specification<Company>();
        spec.Query
            .Include(x => x.Stores)
                .ThenInclude(x => x.Products);

        var actual = _evaluator.GetQuery(DbContext.Companies, spec);
        var actualSql = GetQueryString(DbContext, actual);

        var expected = DbContext.Companies
            .Include($"{nameof(Company.Stores)}.{nameof(Store.Products)}");
        var expectedSql = GetQueryString(DbContext, expected);

        actualSql.Should().Be(expectedSql);
    }

    [Fact]
    public void QueriesMatch_GivenNestedThenIncludeExpression()
    {
        var spec = new Specification<Company>();
        spec.Query
            .Include(x => x.Stores)
                .ThenInclude(x => x.Products)
                .ThenInclude(x => x.Images);

        var actual = _evaluator.GetQuery(DbContext.Companies, spec);
        var actualSql = GetQueryString(DbContext, actual);

        var expected = DbContext.Companies
            .Include($"{nameof(Company.Stores)}.{nameof(Store.Products)}.{nameof(Product.Images)}");
        var expectedSql = GetQueryString(DbContext, expected);

        actualSql.Should().Be(expectedSql);
    }

    [Fact]
    public void QueriesMatch_GivenThenIncludeExpressionAfterIncludeExpression()
    {
        var spec = new Specification<Company>();
        spec.Query
            .Include(x => x.Country)
            .Include(x => x.Stores)
                .ThenInclude(x => x.Products);

        var actual = _evaluator.GetQuery(DbContext.Companies, spec);
        var actualSql = GetQueryString(DbContext, actual);

        var expected = DbContext.Companies
            .Include(nameof(Company.Country))
            .Include($"{nameof(Company.Stores)}.{nameof(Store.Products)}");
        var expectedSql = GetQueryString(DbContext, expected);

        actualSql.Should().Be(expectedSql);
    }

    // The second chain starts from a navigation the first chain already includes, so no path EF6 holds ends where it
    // starts.
    [Fact]
    public void QueriesMatch_GivenThenIncludeExpressionsFromSameNavigation()
    {
        var spec = new Specification<Company>();
        spec.Query
            .Include(x => x.Stores)
                .ThenInclude(x => x.Products)
            .Include(x => x.Stores)
                .ThenInclude(x => x.Address);

        var actual = _evaluator.GetQuery(DbContext.Companies, spec);
        var actualSql = GetQueryString(DbContext, actual);

        var expected = DbContext.Companies
            .Include($"{nameof(Company.Stores)}.{nameof(Store.Products)}")
            .Include($"{nameof(Company.Stores)}.{nameof(Store.Address)}");
        var expectedSql = GetQueryString(DbContext, expected);

        actualSql.Should().Be(expectedSql);
    }
}
