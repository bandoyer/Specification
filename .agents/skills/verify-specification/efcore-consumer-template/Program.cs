// The EF Core provider's consumer. It seeds a database of its own on SQL Server, then enumerates AsAsyncEnumerable
// through RepositoryBase<T> and through ContextFactoryRepositoryBaseOfT, whose factory counts the contexts it makes and
// how many of them are disposed. It also reads single results through ContextFactoryRepositoryBaseOfT. Then it queries a
// shop through the DbSet extensions (Shop.cs), and streams a projection through both repositories and the DbSet
// (Projected.cs). It prints one "label: result" line per query, and drops the database.
// `verify run` runs it. The output holds no ids or paths, so two trees' outputs compare with diff.
using System.Linq.Expressions;
using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

// Never the test fixtures' SpecificationTestDB_* databases, nor the EF6 consumer's.
const string Catalog = "VerifySpecificationEFCoreConsumer";

var server = Environment.GetEnvironmentVariable("SPECIFICATION_TEST_SQL_SERVER");
if (string.IsNullOrEmpty(server))
{
    Console.Error.WriteLine("SPECIFICATION_TEST_SQL_SERVER is unset; verify run sets it.");
    return 2;
}
var options = new DbContextOptionsBuilder<AtlasContext>()
    .UseSqlServer(new SqlConnectionStringBuilder(server) { InitialCatalog = Catalog }.ToString())
    .Options;

await using (var db = new AtlasContext(options))
{
    await db.Database.EnsureDeletedAsync();
    await db.Database.EnsureCreatedAsync();
    db.Countries.AddRange(new[] { "Norway", "Austria", "Belgium", "Albania", "Andorra" }.Select(name => new Country { Name = name }));
    await db.SaveChangesAsync();
    await Shop.Seed(db);
}

try
{
    await Atlas.Query(options);
    await Shop.Query(options);
    await Projected.Query(options);
}
finally
{
    SqlConnection.ClearAllPools();
    await using var db = new AtlasContext(options);
    await db.Database.EnsureDeletedAsync();
}
return 0;

static class Atlas
{
    public static async Task Query(DbContextOptions<AtlasContext> options)
    {
        await using (var db = new AtlasContext(options))
        {
            var repository = new CountryRepository(db);
            Print("repository-async-enumerable", await Read(repository.AsAsyncEnumerable(new NamesStartingWithA())));
            Print("repository-async-enumerable-cancel", await Read(repository.AsAsyncEnumerable(new NamesStartingWithA()), cancelAfterFirst: true));
        }

        await PrintFactory(options, "factory-async-enumerable", sequence => Read(sequence));
        await PrintFactory(options, "factory-async-enumerable-break", sequence => Read(sequence, breakAfterFirst: true));
        await PrintFactory(options, "factory-async-enumerable-cancel", sequence => Read(sequence, cancelAfterFirst: true));
        await PrintFactory(options, "factory-async-enumerable-twice", async sequence => $"{await Read(sequence)} | {await Read(sequence)}");
        await PrintFactory(options, "factory-async-enumerable-unenumerated", _ => Task.FromResult("not enumerated"));

        // Two of the five names end with "ia": Albania and Austria.
        var cancelled = new CancellationToken(canceled: true);
        await PrintFactoryQuery(options, "factory-single", async repository => (await repository.SingleOrDefaultAsync(new OnlyCountry(c => c.Name == "Norway")))?.Name);
        await PrintFactoryQuery(options, "factory-single-missing", async repository => (await repository.SingleOrDefaultAsync(new OnlyCountry(c => c.Name == "Peru")))?.Name);
        await PrintFactoryQuery(options, "factory-single-two-matches", async repository => (await repository.SingleOrDefaultAsync(new OnlyCountry(c => c.Name.EndsWith("ia"))))?.Name);
        await PrintFactoryQuery(options, "factory-single-cancel", async repository => (await repository.SingleOrDefaultAsync(new OnlyCountry(c => c.Name == "Norway"), cancelled))?.Name);
        await PrintFactoryQuery(options, "factory-single-projected", repository => repository.SingleOrDefaultAsync(new OnlyCountryName(c => c.Name == "Norway")));
        await PrintFactoryQuery(options, "factory-single-projected-missing", repository => repository.SingleOrDefaultAsync(new OnlyCountryName(c => c.Name == "Peru")));
        await PrintFactoryQuery(options, "factory-single-projected-two-matches", repository => repository.SingleOrDefaultAsync(new OnlyCountryName(c => c.Name.EndsWith("ia"))));
        await PrintFactoryQuery(options, "factory-single-projected-cancel", repository => repository.SingleOrDefaultAsync(new OnlyCountryName(c => c.Name == "Norway"), cancelled));
        await PrintFactoryQuery(options, "factory-first-two-matches", async repository => (await repository.FirstOrDefaultAsync(new NamesEndingWithIa()))?.Name);
    }

    // Each label gets a factory of its own, so its count holds only the contexts that label's query made.
    private static async Task PrintFactory(DbContextOptions<AtlasContext> options, string label, Func<IAsyncEnumerable<Country>, Task<string>> read)
    {
        var factory = new CountingFactory(options);
        var result = await read(new FactoryCountryRepository(factory).AsAsyncEnumerable(new NamesStartingWithA()));
        Print(label, $"{result}; {factory.Tally}");
    }

    // The same for a query that returns one result: the name it read, <null>, or the type of the exception it threw.
    private static async Task PrintFactoryQuery(DbContextOptions<AtlasContext> options, string label, Func<FactoryCountryRepository, Task<string?>> query)
    {
        var factory = new CountingFactory(options);
        string result;
        try
        {
            result = await query(new FactoryCountryRepository(factory)) ?? "<null>";
        }
        catch (Exception e)
        {
            result = e.GetType().Name;
        }
        Print(label, $"{result}; {factory.Tally}");
    }

    // The names an await foreach reads, then how it stopped when it stopped early: "then break", or the exception's type.
    private static async Task<string> Read(IAsyncEnumerable<Country> sequence, bool breakAfterFirst = false, bool cancelAfterFirst = false)
    {
        var names = new List<string>();
        var ending = "";
        using var cts = new CancellationTokenSource();
        try
        {
            await foreach (var country in sequence.WithCancellation(cts.Token))
            {
                names.Add(country.Name);
                if (breakAfterFirst)
                {
                    ending = " then break";
                    break;
                }
                if (cancelAfterFirst) cts.Cancel();
            }
        }
        catch (Exception e)
        {
            ending = $" then {e.GetType().Name}";
        }
        return (names.Count == 0 ? "<none>" : string.Join(", ", names)) + ending;
    }

    private static void Print(string label, string result) => Console.WriteLine($"{label}: {result}");
}

public sealed class Country
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

public sealed class NamesStartingWithA : Specification<Country>
{
    public NamesStartingWithA() => Query.Where(country => country.Name.StartsWith("A")).OrderBy(country => country.Name);
}

public sealed class NamesEndingWithIa : Specification<Country>
{
    public NamesEndingWithIa() => Query.Where(country => country.Name.EndsWith("ia")).OrderBy(country => country.Name);
}

public sealed class OnlyCountry : SingleResultSpecification<Country>
{
    public OnlyCountry(Expression<Func<Country, bool>> criteria) => Query.Where(criteria);
}

public sealed class OnlyCountryName : SingleResultSpecification<Country, string>
{
    public OnlyCountryName(Expression<Func<Country, bool>> criteria)
    {
        Query.Where(criteria);
        Query.Select(country => country.Name);
    }
}

public sealed class AtlasContext(DbContextOptions<AtlasContext> options) : DbContext(options)
{
    public bool Disposed { get; private set; }
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Product> Products => Set<Product>();

    // decimal(18,2), the EF6 consumer's column.
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);

    public override void Dispose()
    {
        Disposed = true;
        base.Dispose();
    }

    public override ValueTask DisposeAsync()
    {
        Disposed = true;
        return base.DisposeAsync();
    }
}

// Makes a new context on each call, as AddDbContextFactory's factory does, and keeps each one to count.
public sealed class CountingFactory(DbContextOptions<AtlasContext> options) : IDbContextFactory<AtlasContext>
{
    private readonly List<AtlasContext> _made = [];

    public string Tally => $"{_made.Count} context{(_made.Count == 1 ? "" : "s")} made, {_made.Count(context => context.Disposed)} disposed";

    public AtlasContext CreateDbContext()
    {
        var context = new AtlasContext(options);
        _made.Add(context);
        return context;
    }
}

public sealed class CountryRepository(AtlasContext context) : RepositoryBase<Country>(context);

public sealed class FactoryCountryRepository(IDbContextFactory<AtlasContext> factory)
    : ContextFactoryRepositoryBaseOfT<Country, AtlasContext>(factory);
