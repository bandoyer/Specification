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

    [Fact]
    public async Task AsAsyncEnumerable_ReturnsProjections_GivenProjectionSpec()
    {
        await SeedRangeAsync(new[]
        {
            new Country { Name = "a", No = 1 },
            new Country { Name = "c", No = 3 },
            new Country { Name = "b", No = 2 },
        });

        var spec = new Specification<Country, string?>();
        spec.Query
            .Where(x => x.No > 1)
            .OrderBy(x => x.No)
            .Select(x => x.Name);

        var result = new List<string?>();
        await foreach (string? name in DbContext.Countries.AsAsyncEnumerable(spec))
        {
            result.Add(name);
        }

        result.Should().Equal("b", "c");
    }

    [Fact]
    public void AsAsyncEnumerable_ThrowsSelectorNotFoundExceptionWhenCalled_GivenProjectionSpecWithoutSelector()
    {
        var spec = new Specification<Country, string?>();

        Func<IAsyncEnumerable<string?>> sut = () => DbContext.Countries.AsAsyncEnumerable(spec);

        sut.Should().Throw<SelectorNotFoundException>();
    }

    [Fact]
    public void AsAsyncEnumerable_ThrowsArgumentNullExceptionWhenCalled_GivenNullProjectionSpec()
    {
        var sut = () => DbContext.Countries.AsAsyncEnumerable<Country, string?>(null!);

        sut.Should().Throw<ArgumentNullException>().WithParameterName("specification");
    }
}
