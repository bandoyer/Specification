using System.Linq.Expressions;
using System.Threading;

namespace Tests.Repositories;

[Collection("SharedCollection")]
public class ContextFactoryRepository_FirstTests(TestFactory factory) : IntegrationTest(factory)
{
    private readonly RecordingContextFactory _contextFactory = new(factory.DbContextOptions);

    [Fact]
    public async Task FirstOrDefaultAsync_ReturnsFirstItemAndDisposesContext_GivenTwoMatches()
    {
        await SeedCountriesAsync();
        var repo = new ContextFactoryRepository(_contextFactory);
        var spec = new Specification<Country>();
        spec.Query
            .Where(x => x.Name!.StartsWith("b"))
            .OrderBy(x => x.Name);

        var result = await repo.FirstOrDefaultAsync(spec);

        result.Should().NotBeNull();
        result!.Name.Should().Be("b1");
        _contextFactory.Made.Should().ContainSingle().Which.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task SingleOrDefaultAsync_ReturnsItemAndDisposesContext_GivenOneMatch()
    {
        await SeedCountriesAsync();
        var repo = new ContextFactoryRepository(_contextFactory);

        var result = await repo.SingleOrDefaultAsync(Only(x => x.Name == "a"));

        result.Should().NotBeNull();
        result!.Name.Should().Be("a");
        _contextFactory.Made.Should().ContainSingle().Which.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task SingleOrDefaultAsync_ReturnsNullAndDisposesContext_GivenNoMatch()
    {
        await SeedCountriesAsync();
        var repo = new ContextFactoryRepository(_contextFactory);

        var result = await repo.SingleOrDefaultAsync(Only(x => x.Name == "c"));

        result.Should().BeNull();
        _contextFactory.Made.Should().ContainSingle().Which.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task SingleOrDefaultAsync_ThrowsInvalidOperationExceptionAndDisposesContext_GivenTwoMatches()
    {
        await SeedCountriesAsync();
        var repo = new ContextFactoryRepository(_contextFactory);

        var sut = () => repo.SingleOrDefaultAsync(Only(x => x.Name!.StartsWith("b")));

        await sut.Should().ThrowAsync<InvalidOperationException>();
        _contextFactory.Made.Should().ContainSingle().Which.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task SingleOrDefaultAsync_ThrowsOperationCanceledExceptionAndDisposesContext_GivenCancelledToken()
    {
        await SeedCountriesAsync();
        var repo = new ContextFactoryRepository(_contextFactory);

        var sut = () => repo.SingleOrDefaultAsync(Only(x => x.Name == "a"), new CancellationToken(canceled: true));

        await sut.Should().ThrowAsync<OperationCanceledException>();
        _contextFactory.Made.Should().ContainSingle().Which.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task SingleOrDefaultAsync_ReturnsProjectionAndDisposesContext_GivenProjectionAndOneMatch()
    {
        await SeedCountriesAsync();
        var repo = new ContextFactoryRepository(_contextFactory);

        var result = await repo.SingleOrDefaultAsync(OnlyName(x => x.Name == "a"));

        result.Should().Be("a");
        _contextFactory.Made.Should().ContainSingle().Which.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task SingleOrDefaultAsync_ReturnsNullAndDisposesContext_GivenProjectionAndNoMatch()
    {
        await SeedCountriesAsync();
        var repo = new ContextFactoryRepository(_contextFactory);

        var result = await repo.SingleOrDefaultAsync(OnlyName(x => x.Name == "c"));

        result.Should().BeNull();
        _contextFactory.Made.Should().ContainSingle().Which.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task SingleOrDefaultAsync_ThrowsInvalidOperationExceptionAndDisposesContext_GivenProjectionAndTwoMatches()
    {
        await SeedCountriesAsync();
        var repo = new ContextFactoryRepository(_contextFactory);

        var sut = () => repo.SingleOrDefaultAsync(OnlyName(x => x.Name!.StartsWith("b")));

        await sut.Should().ThrowAsync<InvalidOperationException>();
        _contextFactory.Made.Should().ContainSingle().Which.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task SingleOrDefaultAsync_ThrowsOperationCanceledExceptionAndDisposesContext_GivenProjectionAndCancelledToken()
    {
        await SeedCountriesAsync();
        var repo = new ContextFactoryRepository(_contextFactory);

        var sut = () => repo.SingleOrDefaultAsync(OnlyName(x => x.Name == "a"), new CancellationToken(canceled: true));

        await sut.Should().ThrowAsync<OperationCanceledException>();
        _contextFactory.Made.Should().ContainSingle().Which.IsDisposed.Should().BeTrue();
    }

    // b2 goes in before b1, so a query that returns the first row it reads, without the OrderBy, gets b2.
    private Task SeedCountriesAsync() => SeedRangeAsync<Country>(
    [
        new() { Name = "a" },
        new() { Name = "b2" },
        new() { Name = "b1" },
    ]);

    private static SingleResultSpecification<Country> Only(Expression<Func<Country, bool>> criteria)
    {
        var spec = new SingleResultSpecification<Country>();
        spec.Query
            .Where(criteria);
        return spec;
    }

    private static SingleResultSpecification<Country, string?> OnlyName(Expression<Func<Country, bool>> criteria)
    {
        var spec = new SingleResultSpecification<Country, string?>();
        spec.Query
            .Where(criteria)
            .Select(x => x.Name);
        return spec;
    }

    private sealed class ContextFactoryRepository(IDbContextFactory<TestDbContext> dbContextFactory)
        : ContextFactoryRepositoryBaseOfT<Country, TestDbContext>(dbContextFactory);
}
