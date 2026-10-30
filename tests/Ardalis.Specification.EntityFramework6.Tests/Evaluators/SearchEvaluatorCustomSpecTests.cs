using System.Data.Entity;
using NSubstitute;

namespace Tests.Evaluators;

// This is a special case where users have custom ISpecification<T> implementation but use our evaluator.
[Collection("SharedCollection")]
public class SearchEvaluatorCustomSpecTests(TestFactory factory) : IntegrationTest(factory)
{
    private static readonly SearchEvaluator _evaluator = SearchEvaluator.Instance;

    [Fact]
    public void QueriesMatch_GivenNoSearch()
    {
        var spec = Substitute.For<ISpecification<Store>>();
        spec.WhereExpressions.Returns(
        [
            new WhereExpressionInfo<Store>(x => x.Id > 0)
        ]);

        var actual = _evaluator.GetQuery(DbContext.Stores, spec);
        var actualSql = GetQueryString(DbContext, actual);

        var expected = DbContext.Stores.AsQueryable();
        var expectedSql = GetQueryString(DbContext, expected);

        actualSql.Should().Be(expectedSql);
    }

    [Fact]
    public void QueriesMatch_GivenSingleSearch()
    {
        var storeTerm = "ab1";

        var spec = Substitute.For<ISpecification<Store>>();
        spec.WhereExpressions.Returns(
        [
            new WhereExpressionInfo<Store>(x => x.Id > 0)
        ]);
        spec.SearchCriterias.Returns(
        [
            new SearchExpressionInfo<Store>(x => x.Name, $"%{storeTerm}%")
        ]);

        var actual = _evaluator.GetQuery(DbContext.Stores, spec);
        var actualSql = GetQueryString(DbContext, actual);

        // EF6 writes "%" + storeTerm + "%" inside a query as a concatenation in SQL, so the pattern comes from a local.
        var storePattern = $"%{storeTerm}%";
        var expected = DbContext.Stores
            .Where(x => DbFunctions.Like(x.Name, storePattern));
        var expectedSql = GetQueryString(DbContext, expected);

        actualSql.Should().Be(expectedSql);
    }

    [Fact]
    public void QueriesMatch_GivenMultipleSearch()
    {
        var storeTerm = "ab1";
        var companyTerm = "ab2";
        var countryTerm = "ab3";
        var streetTerm = "ab4";

        var spec = Substitute.For<ISpecification<Store>>();
        spec.WhereExpressions.Returns(
        [
            new WhereExpressionInfo<Store>(x => x.Id > 0)
        ]);
        spec.SearchCriterias.Returns(
        [
            new SearchExpressionInfo<Store>(x => x.Name, $"%{storeTerm}%"),
            new SearchExpressionInfo<Store>(x => x.Company.Name, $"%{companyTerm}%"),
            new SearchExpressionInfo<Store>(x => x.Address.Street, $"%{streetTerm}%", 2),
            new SearchExpressionInfo<Store>(x => x.Company.Country.Name, $"%{countryTerm}%", 3)
        ]);

        var actual = _evaluator.GetQuery(DbContext.Stores, spec);
        var actualSql = GetQueryString(DbContext, actual);

        // EF6 writes "%" + term + "%" inside a query as a concatenation in SQL, so each pattern comes from a local.
        var storePattern = $"%{storeTerm}%";
        var companyPattern = $"%{companyTerm}%";
        var countryPattern = $"%{countryTerm}%";
        var streetPattern = $"%{streetTerm}%";
        var expected = DbContext.Stores
            .Where(x => DbFunctions.Like(x.Name, storePattern)
                    || DbFunctions.Like(x.Company.Name, companyPattern))
            .Where(x => DbFunctions.Like(x.Address.Street, streetPattern))
            .Where(x => DbFunctions.Like(x.Company.Country.Name, countryPattern));
        var expectedSql = GetQueryString(DbContext, expected);

        actualSql.Should().Be(expectedSql);
    }
}
