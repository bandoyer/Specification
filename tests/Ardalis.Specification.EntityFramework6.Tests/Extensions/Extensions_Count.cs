using System.Data.Entity;

namespace Tests.Extensions;

[Collection("SharedCollection")]
public class Extensions_Count(TestFactory factory) : IntegrationTest(factory)
{
    // A default or null argument binds to the specification overload, ahead of EF's CancellationToken and predicate
    // overloads, so it throws rather than reading the row. The ! changes no binding.
    [Fact]
    public async Task CountAsync_ThrowsArgumentNullException_GivenDefault()
    {
        await SeedRangeAsync(new[] { new Country { Name = "a" } });

        Func<Task> sut = () => DbContext.Countries.CountAsync(default!);

        await sut.Should().ThrowAsync<ArgumentNullException>().WithParameterName("specification");
    }

    [Fact]
    public async Task CountAsync_ThrowsArgumentNullException_GivenNull()
    {
        await SeedRangeAsync(new[] { new Country { Name = "a" } });

        Func<Task> sut = () => DbContext.Countries.CountAsync(null!);

        await sut.Should().ThrowAsync<ArgumentNullException>().WithParameterName("specification");
    }

    [Fact]
    public async Task CountAsync_ReturnsMatchCount_GivenSpec()
    {
        await SeedRangeAsync(new[]
        {
            new Country { Name = "a" },
            new Country { Name = "b" },
            new Country { Name = "b" },
            new Country { Name = "c" },
        });

        var spec = new Specification<Country>();
        spec.Query
            .Where(x => x.Name == "b");

        var result = await DbContext.Countries.CountAsync(spec);

        result.Should().Be(2);
    }

    [Fact]
    public async Task CountAsync_IgnoresPagination_GivenSpecWithPagination()
    {
        await SeedRangeAsync(new[]
        {
            new Country { Name = "a", No = 1 },
            new Country { Name = "b", No = 2 },
            new Country { Name = "b", No = 3 },
            new Country { Name = "b", No = 4 },
        });

        var spec = new Specification<Country>();
        spec.Query
            .Where(x => x.Name == "b")
            .OrderBy(x => x.No)
            .Skip(1)
            .Take(1);

        var result = await DbContext.Countries.CountAsync(spec);

        result.Should().Be(3);
    }
}
