namespace Tests.Extensions;

[Collection("SharedCollection")]
public class Extensions_SingleOrDefault(TestFactory factory) : IntegrationTest(factory)
{
    // A default or null argument binds to the specification overload, ahead of EF's CancellationToken and predicate
    // overloads, so it throws rather than reading the row. The ! changes no binding.
    [Fact]
    public async Task SingleOrDefaultAsync_ThrowsArgumentNullException_GivenDefault()
    {
        await SeedRangeAsync(new[] { new Country { Name = "a" } });

        Func<Task> sut = () => DbContext.Countries.SingleOrDefaultAsync(default!);

        await sut.Should().ThrowAsync<ArgumentNullException>().WithParameterName("specification");
    }

    [Fact]
    public async Task SingleOrDefaultAsync_ThrowsArgumentNullException_GivenNull()
    {
        await SeedRangeAsync(new[] { new Country { Name = "a" } });

        Func<Task> sut = () => DbContext.Countries.SingleOrDefaultAsync(null!);

        await sut.Should().ThrowAsync<ArgumentNullException>().WithParameterName("specification");
    }

    [Fact]
    public async Task SingleOrDefaultAsync_ReturnsMatch_GivenSpec()
    {
        await SeedRangeAsync(new[]
        {
            new Country { Name = "a", No = 1 },
            new Country { Name = "b", No = 2 },
            new Country { Name = "c", No = 3 },
        });

        var spec = new SingleResultSpecification<Country>();
        spec.Query
            .Where(x => x.Name == "b");

        var result = await DbContext.Countries.SingleOrDefaultAsync(spec);

        result.Should().NotBeNull();
        result!.No.Should().Be(2);
    }

    [Fact]
    public async Task SingleOrDefaultAsync_ReturnsNull_GivenNoMatch()
    {
        await SeedRangeAsync(new[] { new Country { Name = "a" } });

        var spec = new SingleResultSpecification<Country>();
        spec.Query
            .Where(x => x.Name == "b");

        var result = await DbContext.Countries.SingleOrDefaultAsync(spec);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SingleOrDefaultAsync_ThrowsInvalidOperationException_GivenTwoMatches()
    {
        await SeedRangeAsync(new[]
        {
            new Country { Name = "b", No = 1 },
            new Country { Name = "b", No = 2 },
        });

        var spec = new SingleResultSpecification<Country>();
        spec.Query
            .Where(x => x.Name == "b");

        Func<Task> sut = () => DbContext.Countries.SingleOrDefaultAsync(spec);

        await sut.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SingleOrDefaultAsync_ReturnsProjection_GivenProjectionSpec()
    {
        await SeedRangeAsync(new[]
        {
            new Country { Name = "a", No = 1 },
            new Country { Name = "b", No = 2 },
        });

        var spec = new SingleResultSpecification<Country, string?>();
        spec.Query
            .Where(x => x.No == 2)
            .Select(x => x.Name);

        var result = await DbContext.Countries.SingleOrDefaultAsync(spec);

        result.Should().Be("b");
    }

    [Fact]
    public async Task SingleOrDefaultAsync_ReturnsNull_GivenProjectionSpecAndNoMatch()
    {
        await SeedRangeAsync(new[] { new Country { Name = "a", No = 1 } });

        var spec = new SingleResultSpecification<Country, string?>();
        spec.Query
            .Where(x => x.No == 2)
            .Select(x => x.Name);

        var result = await DbContext.Countries.SingleOrDefaultAsync(spec);

        result.Should().BeNull();
    }
}
