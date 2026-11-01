using System.Data.Entity;

namespace Tests.Extensions;

[Collection("SharedCollection")]
public class Extensions_FirstOrDefault(TestFactory factory) : IntegrationTest(factory)
{
    // A default or null argument binds to the specification overload, ahead of EF's CancellationToken and predicate
    // overloads, so it throws rather than reading the row. The ! changes no binding.
    [Fact]
    public async Task FirstOrDefaultAsync_ThrowsArgumentNullException_GivenDefault()
    {
        await SeedRangeAsync(new[] { new Country { Name = "a" } });

        Func<Task> sut = () => DbContext.Countries.FirstOrDefaultAsync(default!);

        await sut.Should().ThrowAsync<ArgumentNullException>().WithParameterName("specification");
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ThrowsArgumentNullException_GivenNull()
    {
        await SeedRangeAsync(new[] { new Country { Name = "a" } });

        Func<Task> sut = () => DbContext.Countries.FirstOrDefaultAsync(null!);

        await sut.Should().ThrowAsync<ArgumentNullException>().WithParameterName("specification");
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ReturnsFirstMatch_GivenSpec()
    {
        await SeedRangeAsync(new[]
        {
            new Country { Name = "a", No = 1 },
            new Country { Name = "b", No = 3 },
            new Country { Name = "b", No = 2 },
        });

        var spec = new Specification<Country>();
        spec.Query
            .Where(x => x.Name == "b")
            .OrderBy(x => x.No);

        var result = await DbContext.Countries.FirstOrDefaultAsync(spec);

        result.Should().NotBeNull();
        result!.No.Should().Be(2);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ReturnsNull_GivenNoMatch()
    {
        await SeedRangeAsync(new[] { new Country { Name = "a" } });

        var spec = new Specification<Country>();
        spec.Query
            .Where(x => x.Name == "b");

        var result = await DbContext.Countries.FirstOrDefaultAsync(spec);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ReturnsFirstProjection_GivenProjectionSpec()
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

        var result = await DbContext.Countries.FirstOrDefaultAsync(spec);

        result.Should().Be("b");
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ReturnsNull_GivenProjectionSpecAndNoMatch()
    {
        await SeedRangeAsync(new[] { new Country { Name = "a", No = 1 } });

        var spec = new Specification<Country, string?>();
        spec.Query
            .Where(x => x.No > 1)
            .Select(x => x.Name);

        var result = await DbContext.Countries.FirstOrDefaultAsync(spec);

        result.Should().BeNull();
    }
}
