using System.Data.Entity;

namespace Tests.Evaluators;

[Collection("SharedCollection")]
public class SearchEvaluatorTests(TestFactory factory) : IntegrationTest(factory)
{
    private static readonly SearchEvaluator _evaluator = SearchEvaluator.Instance;

    [Fact]
    public void QueriesMatch_GivenNoSearch()
    {
        var spec = new Specification<Company>();
        spec.Query
            .Where(x => x.Id > 0);

        var actual = _evaluator.GetQuery(DbContext.Companies, spec);
        var actualSql = GetQueryString(DbContext, actual);

        var expected = DbContext.Companies.AsQueryable();
        var expectedSql = GetQueryString(DbContext, expected);

        actualSql.Should().Be(expectedSql);
    }

    [Fact]
    public void QueriesMatch_GivenSingleSearch()
    {
        var storeTerm = "ab1";

        var spec = new Specification<Company>();
        spec.Query
            .Where(x => x.Id > 0)
            .Search(x => x.Name, $"%{storeTerm}%");

        var actual = _evaluator.GetQuery(DbContext.Companies, spec);
        var actualSql = GetQueryString(DbContext, actual);

        // EF6 writes "%" + storeTerm + "%" inside a query as a concatenation in SQL, so the pattern comes from a local.
        var storePattern = $"%{storeTerm}%";
        var expected = DbContext.Companies
            .Where(x => DbFunctions.Like(x.Name, storePattern));
        var expectedSql = GetQueryString(DbContext, expected);

        actualSql.Should().Be(expectedSql);
    }

    // A literal would quote, prefix, or size each of these differently in the SQL.
    public static TheoryData<string> SearchTermForms => new()
    {
        "%O'Brien%",
        "%Tiranë%",
        "%",
        "%" + new string('z', 3998) + "%", // 4000 characters, SQL Server's longest LIKE pattern
    };

    [Theory]
    [MemberData(nameof(SearchTermForms))]
    public void QueriesMatch_GivenSingleSearchWithTermForm(string term)
    {
        var spec = new Specification<Company>();
        spec.Query
            .Search(x => x.Name, term);

        var actual = _evaluator.GetQuery(DbContext.Companies, spec);
        var actualSql = GetQueryString(DbContext, actual);

        var expected = DbContext.Companies
            .Where(x => DbFunctions.Like(x.Name, term));
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

        var spec = new Specification<Store>();
        spec.Query
            .Where(x => x.Id > 0)
            .Search(x => x.Name, $"%{storeTerm}%")
            .Search(x => x.Company.Name, $"%{companyTerm}%")
            .Search(x => x.Company.Country.Name, $"%{countryTerm}%", 3)
            .Search(x => x.Address.Street, $"%{streetTerm}%", 2);

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
