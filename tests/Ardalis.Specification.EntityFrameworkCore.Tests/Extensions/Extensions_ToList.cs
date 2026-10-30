using System.Threading;

namespace Tests.Extensions;

[Collection("SharedCollection")]
public class Extensions_ToList(TestFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task ToListAsync_GivenSpec()
    {
        var expectedName = "Country3";

        var countries = new List<Country>
        {
            new()
            {
                Name = "Country1",
            },
            new()
            {
                Name = "Country2",
            },
            new()
            {
                Name = expectedName,
            }
        };

        await SeedRangeAsync(countries);


        var spec = new Specification<Country>();
        spec.Query
            .Where(x => x.Name == expectedName);

        var result = await DbContext
            .Countries
            .ToListAsync(spec, CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Name.Should().Be(expectedName);
    }

    [Fact]
    public async Task ToListAsync_GivenSpecAndPostProcessingAction()
    {
        var expectedName = "Country3";

        var countries = new List<Country>
        {
            new()
            {
                Name = "Country1",
            },
            new()
            {
                Name = "Country2",
            },
            new()
            {
                Name = expectedName,
            }
        };

        await SeedRangeAsync(countries);


        var spec = new Specification<Country>();
        spec.Query
            .PostProcessingAction(x => x.Where(x => x.Name == expectedName));

        var result = await DbContext
            .Countries
            .ToListAsync(spec, CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Name.Should().Be(expectedName);
    }

    [Fact]
    public async Task ToEnumerableAsync_GivenSpec()
    {
        var expectedName = "Country3";

        var countries = new List<Country>
        {
            new()
            {
                Name = "Country1",
            },
            new()
            {
                Name = "Country2",
            },
            new()
            {
                Name = expectedName,
            }
        };

        await SeedRangeAsync(countries);


        var spec = new Specification<Country>();
        spec.Query
            .Where(x => x.Name == expectedName);

        var result = await DbContext
            .Countries
            .ToEnumerableAsync(spec, CancellationToken.None);

        result.Should().ContainSingle();
        result.First().Name.Should().Be(expectedName);
    }

    [Fact]
    public async Task ToEnumerableAsync_GivenSpecAndPostProcessingAction()
    {
        var expectedName = "Country3";

        var countries = new List<Country>
        {
            new()
            {
                Name = "Country1",
            },
            new()
            {
                Name = "Country2",
            },
            new()
            {
                Name = expectedName,
            }
        };

        await SeedRangeAsync(countries);

        var spec = new Specification<Country>();
        spec.Query
            .PostProcessingAction(x => x.Where(x => x.Name == expectedName));

        var result = await DbContext
            .Countries
            .ToEnumerableAsync(spec, CancellationToken.None);

        result.Should().ContainSingle();
        result.First().Name.Should().Be(expectedName);
    }

    [Fact]
    public async Task ToListAsync_ReturnsProjections_GivenProjectionSpec()
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

        List<string?> result = await DbContext.Countries.ToListAsync(spec, CancellationToken.None);

        result.Should().Equal("b", "c");
    }

    [Fact]
    public async Task ToListAsync_ReturnsProjections_GivenProjectionSpecAndPostProcessingAction()
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
        spec.Query
            .PostProcessingAction(x => x.Reverse());

        var result = await DbContext.Countries.ToListAsync(spec, CancellationToken.None);

        result.Should().Equal("c", "b");
    }

    [Fact]
    public async Task ToListAsync_ThrowsSelectorNotFoundException_GivenProjectionSpecWithoutSelector()
    {
        await SeedRangeAsync(new[] { new Country { Name = "a" } });

        var spec = new Specification<Country, string?>();

        Func<Task> sut = () => DbContext.Countries.ToListAsync(spec, CancellationToken.None);

        await sut.Should().ThrowAsync<SelectorNotFoundException>();
    }

    [Fact]
    public async Task ToEnumerableAsync_ReturnsProjections_GivenProjectionSpec()
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

        IEnumerable<string?> result = await DbContext.Countries.ToEnumerableAsync(spec, CancellationToken.None);

        result.Should().Equal("b", "c");
    }

    [Fact]
    public async Task ToEnumerableAsync_ReturnsProjections_GivenProjectionSpecAndPostProcessingAction()
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
        spec.Query
            .PostProcessingAction(x => x.Reverse());

        var result = await DbContext.Countries.ToEnumerableAsync(spec, CancellationToken.None);

        result.Should().Equal("c", "b");
    }
}
