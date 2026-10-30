using System.Data.Entity;
using NSubstitute;

namespace Tests.Evaluators;

[Collection("SharedCollection")]
public class IncludeEvaluatorCustomSpecTests(TestFactory factory) : IntegrationTest(factory)
{
    private static readonly IncludeEvaluator _evaluator = IncludeEvaluator.Instance;

    [Fact]
    public void QueriesMatch_GivenNoIncludeExpression()
    {
        var spec = Substitute.For<ISpecification<Store>>();

        var actual = _evaluator.GetQuery(DbContext.Stores, spec);
        var actualSql = GetQueryString(DbContext, actual);

        var expected = DbContext.Stores.AsQueryable();
        var expectedSql = GetQueryString(DbContext, expected);

        actualSql.Should().Be(expectedSql);
    }

    [Fact]
    public void QueriesMatch_GivenThenIncludeExpression()
    {
        var chains = new Specification<Store>();
        chains.Query
            .Include(x => x.Products)
                .ThenInclude(x => x.Images)
            .Include(x => x.Company)
                .ThenInclude(x => x.Country);
        var spec = Substitute.For<ISpecification<Store>>();
        spec.IncludeExpressions.Returns(chains.IncludeExpressions);

        var actual = _evaluator.GetQuery(DbContext.Stores, spec);
        var actualSql = GetQueryString(DbContext, actual);

        var expected = DbContext.Stores
            .Include($"{nameof(Store.Products)}.{nameof(Product.Images)}")
            .Include($"{nameof(Store.Company)}.{nameof(Company.Country)}");
        var expectedSql = GetQueryString(DbContext, expected);

        actualSql.Should().Be(expectedSql);
    }
}
