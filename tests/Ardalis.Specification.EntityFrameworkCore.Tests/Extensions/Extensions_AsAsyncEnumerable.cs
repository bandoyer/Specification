namespace Tests.Extensions;

[Collection("SharedCollection")]
public class Extensions_AsAsyncEnumerable(TestFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task AsAsyncEnumerable_ReturnsMatches_GivenSpec()
    {
        await SeedRangeAsync(new[]
        {
            new Country { Name = "a", No = 1 },
            new Country { Name = "c", No = 3 },
            new Country { Name = "b", No = 2 },
        });

        var spec = new Specification<Country>();
        spec.Query
            .Where(x => x.No > 1)
            .OrderBy(x => x.No);

        var result = new List<Country>();
        await foreach (var country in DbContext.Countries.AsAsyncEnumerable(spec))
        {
            result.Add(country);
        }

        result.Select(x => x.Name).Should().Equal("b", "c");
    }
}
