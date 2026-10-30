using System.Threading;

namespace Tests.Repositories;

[Collection("SharedCollection")]
public class ContextFactoryRepository_AsAsyncEnumerableTests(TestFactory factory) : IntegrationTest(factory)
{
    private readonly RecordingContextFactory _contextFactory = new(factory.DbContextOptions);

    [Fact]
    public async Task AsAsyncEnumerable_ReturnsFilteredItemsAndDisposesContext_GivenSpec()
    {
        await SeedCountriesAsync();
        var repo = new ContextFactoryRepository(_contextFactory);

        var names = await ReadNamesAsync(repo.AsAsyncEnumerable(NamesStartingWithB()));

        names.Should().Equal("b1", "b2", "b3");
        _contextFactory.Made.Should().ContainSingle().Which.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task AsAsyncEnumerable_DisposesContext_GivenBreakAfterFirstItem()
    {
        await SeedCountriesAsync();
        var repo = new ContextFactoryRepository(_contextFactory);

        var names = new List<string?>();
        await foreach (var item in repo.AsAsyncEnumerable(NamesStartingWithB()))
        {
            names.Add(item.Name);
            break;
        }

        names.Should().Equal("b1");
        _contextFactory.Made.Should().ContainSingle().Which.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task AsAsyncEnumerable_ThrowsAndDisposesContext_GivenCancellationAfterFirstItem()
    {
        await SeedCountriesAsync();
        var repo = new ContextFactoryRepository(_contextFactory);
        using var cts = new CancellationTokenSource();

        var names = new List<string?>();
        var enumerate = async () =>
        {
            await foreach (var item in repo.AsAsyncEnumerable(NamesStartingWithB()).WithCancellation(cts.Token))
            {
                names.Add(item.Name);
                cts.Cancel();
            }
        };

        await enumerate.Should().ThrowAsync<OperationCanceledException>();
        names.Should().Equal("b1");
        _contextFactory.Made.Should().ContainSingle().Which.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task AsAsyncEnumerable_UsesContextPerEnumeration_GivenSequenceEnumeratedTwice()
    {
        await SeedCountriesAsync();
        var repo = new ContextFactoryRepository(_contextFactory);

        var sequence = repo.AsAsyncEnumerable(NamesStartingWithB());
        var first = await ReadNamesAsync(sequence);
        var second = await ReadNamesAsync(sequence);

        first.Should().Equal("b1", "b2", "b3");
        second.Should().Equal("b1", "b2", "b3");
        _contextFactory.Made.Should().HaveCount(2).And.OnlyContain(context => context.IsDisposed);
    }

    [Fact]
    public void AsAsyncEnumerable_LeavesNoContextUndisposed_GivenSequenceNeverEnumerated()
    {
        var repo = new ContextFactoryRepository(_contextFactory);

        _ = repo.AsAsyncEnumerable(NamesStartingWithB());

        _contextFactory.Made.Where(context => !context.IsDisposed).Should().BeEmpty();
    }

    [Fact]
    public void AsAsyncEnumerable_ThrowsArgumentNullExceptionWhenCalled_GivenNullSpec()
    {
        var repo = new ContextFactoryRepository(_contextFactory);

        var sut = () => repo.AsAsyncEnumerable(null!);

        sut.Should().Throw<ArgumentNullException>().WithParameterName("specification");
        _contextFactory.Made.Where(context => !context.IsDisposed).Should().BeEmpty();
    }

    private Task SeedCountriesAsync() => SeedRangeAsync<Country>(
    [
        new() { Name = "a" },
        new() { Name = "b2" },
        new() { Name = "c" },
        new() { Name = "b1" },
        new() { Name = "b3" },
    ]);

    private static Specification<Country> NamesStartingWithB()
    {
        var spec = new Specification<Country>();
        spec.Query
            .Search(x => x.Name, "b%")
            .OrderBy(x => x.Name);
        return spec;
    }

    private static async Task<List<string?>> ReadNamesAsync(IAsyncEnumerable<Country> sequence)
    {
        var names = new List<string?>();
        await foreach (var item in sequence)
        {
            names.Add(item.Name);
        }
        return names;
    }

    private sealed class ContextFactoryRepository(IDbContextFactory<TestDbContext> dbContextFactory)
        : ContextFactoryRepositoryBaseOfT<Country, TestDbContext>(dbContextFactory);
}
