using System.Data.Entity;

namespace Tests.Extensions;

[Collection("SharedCollection")]
public class Extensions_Any(TestFactory factory) : IntegrationTest(factory)
{
    // A default or null argument binds to the specification overload, ahead of EF's CancellationToken and predicate
    // overloads, so it throws rather than reading the row. The ! changes no binding.
    [Fact]
    public async Task AnyAsync_ThrowsArgumentNullException_GivenDefault()
    {
        await SeedRangeAsync(new[] { new Country { Name = "a" } });

        Func<Task> sut = () => DbContext.Countries.AnyAsync(default!);

        await sut.Should().ThrowAsync<ArgumentNullException>().WithParameterName("specification");
    }

    [Fact]
    public async Task AnyAsync_ThrowsArgumentNullException_GivenNull()
    {
        await SeedRangeAsync(new[] { new Country { Name = "a" } });

        Func<Task> sut = () => DbContext.Countries.AnyAsync(null!);

        await sut.Should().ThrowAsync<ArgumentNullException>().WithParameterName("specification");
    }

    [Fact]
    public async Task AnyAsync_ReturnsTrue_GivenSpecMatches()
    {
        await SeedRangeAsync(new[]
        {
            new Country { Name = "a" },
            new Country { Name = "b" },
        });

        var spec = new Specification<Country>();
        spec.Query
            .Where(x => x.Name == "b");

        var result = await DbContext.Countries.AnyAsync(spec);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task AnyAsync_ReturnsFalse_GivenNoMatch()
    {
        await SeedRangeAsync(new[] { new Country { Name = "a" } });

        var spec = new Specification<Country>();
        spec.Query
            .Where(x => x.Name == "b");

        var result = await DbContext.Countries.AnyAsync(spec);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AnyAsync_IgnoresPagination_GivenSpecWithPagination()
    {
        await SeedRangeAsync(new[]
        {
            new Country { Name = "a", No = 1 },
            new Country { Name = "b", No = 2 },
        });

        var spec = new Specification<Country>();
        spec.Query
            .Where(x => x.Name == "b")
            .OrderBy(x => x.No)
            .Skip(1);

        var result = await DbContext.Countries.AnyAsync(spec);

        result.Should().BeTrue();
    }
}
