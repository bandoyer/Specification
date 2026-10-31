// The EF Core consumer's projection streams (#16). Its lines stream a Specification<Country, string> with
// AsAsyncEnumerable through IReadRepositoryBase<Country>, over RepositoryBase<T> and over ContextFactoryRepositoryBaseOfT,
// and through the DbSet extension. Each call is written with var, so it also builds against a tree from before #16,
// where it binds to the entity overload. Each read ends with the element type the call returned, (String) or (Country),
// so a diff of the two trees shows which overload each call bound to. The PROJECTED_ASYNC_ENUMERABLE lines need #16's
// overload, and the DBSET_READS lines #11's DbSet extensions.
using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

static class Projected
{
    public static async Task Query(DbContextOptions<AtlasContext> options)
    {
        await using (var db = new AtlasContext(options))
        {
            IReadRepositoryBase<Country> repository = new CountryRepository(db);
            Print("projected-repository", await Read(() => repository.AsAsyncEnumerable(new CountryNames())));
#if PROJECTED_ASYNC_ENUMERABLE
            var typed = new List<string>();
            await foreach (string name in repository.AsAsyncEnumerable(new CountryNames()))
            {
                typed.Add(name);
            }
            Print("projected-repository-typed", string.Join(", ", typed));
#endif
            Print("projected-repository-cancel", await Read(() => repository.AsAsyncEnumerable(new CountryNames()), cancelAfterFirst: true));
            Print("projected-repository-post-processing",
                $"{await Read(() => repository.AsAsyncEnumerable(new CountryNames(dropAll: true)))}; ListAsync: {Names(await repository.ListAsync(new CountryNames(dropAll: true)))}");
            Print("projected-repository-no-select", await Read(() => repository.AsAsyncEnumerable(new CountryNames(select: false))));
#if PROJECTED_ASYNC_ENUMERABLE
            Print("projected-repository-null", await Read(() => repository.AsAsyncEnumerable<string>(null!)));
#endif
        }

        await PrintFactory(options, "projected-factory", repository => Read(() => repository.AsAsyncEnumerable(new CountryNames())));
        await PrintFactory(options, "projected-factory-break", repository => Read(() => repository.AsAsyncEnumerable(new CountryNames()), breakAfterFirst: true));
        await PrintFactory(options, "projected-factory-cancel", repository => Read(() => repository.AsAsyncEnumerable(new CountryNames()), cancelAfterFirst: true));
        await PrintFactory(options, "projected-factory-twice", async repository =>
        {
            var sequence = repository.AsAsyncEnumerable(new CountryNames());
            return $"{await Read(() => sequence)} | {await Read(() => sequence)}";
        });
        await PrintFactory(options, "projected-factory-unenumerated", repository => Task.FromResult(NotEnumerated(repository.AsAsyncEnumerable(new CountryNames()))));
        await PrintFactory(options, "projected-factory-post-processing", repository => Read(() => repository.AsAsyncEnumerable(new CountryNames(dropAll: true))));
        await PrintFactory(options, "projected-factory-no-select", repository => Read(() => repository.AsAsyncEnumerable(new CountryNames(select: false))));
#if PROJECTED_ASYNC_ENUMERABLE
        await PrintFactory(options, "projected-factory-null", repository => Read(() => repository.AsAsyncEnumerable<string>(null!)));
#endif

#if DBSET_READS
        await using (var db = new AtlasContext(options))
        {
            Print("projected-dbset", await Read(() => db.Countries.AsAsyncEnumerable(new CountryNames())));
            Print("projected-dbset-cancel", await Read(() => db.Countries.AsAsyncEnumerable(new CountryNames()), cancelAfterFirst: true));
            Print("projected-dbset-post-processing", await Read(() => db.Countries.AsAsyncEnumerable(new CountryNames(dropAll: true))));
            Print("projected-dbset-no-select", await Read(() => db.Countries.AsAsyncEnumerable(new CountryNames(select: false))));
#if PROJECTED_ASYNC_ENUMERABLE
            Print("projected-dbset-null", await Read(() => db.Countries.AsAsyncEnumerable<Country, string>(null!)));
#endif
        }
#endif
    }

    // Each label gets a factory of its own, so its count holds only the contexts that label's calls made.
    private static async Task PrintFactory(DbContextOptions<AtlasContext> options, string label, Func<IReadRepositoryBase<Country>, Task<string>> query)
    {
        var factory = new CountingFactory(options);
        var result = await query(new FactoryCountryRepository(factory));
        Print(label, $"{result}; {factory.Tally}");
    }

    // Calls AsAsyncEnumerable, then reads the sequence with await foreach. It returns the names read, then how the read
    // stopped early: "then break", or the exception's type. An exception from the call itself returns as "<type> when
    // called", with the parameter an ArgumentException names. The element type the call returned ends the result.
    private static async Task<string> Read<TItem>(Func<IAsyncEnumerable<TItem>> call, bool breakAfterFirst = false, bool cancelAfterFirst = false)
    {
        IAsyncEnumerable<TItem> sequence;
        try
        {
            sequence = call();
        }
        catch (Exception e)
        {
            var parameter = e is ArgumentException { ParamName: { } name } ? $" for {name}" : "";
            return $"{e.GetType().Name}{parameter} when called ({typeof(TItem).Name})";
        }

        var names = new List<string>();
        var ending = "";
        using var cts = new CancellationTokenSource();
        try
        {
            await foreach (var item in sequence.WithCancellation(cts.Token))
            {
                names.Add(item is Country country ? country.Name : $"{item}");
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
        return $"{Names(names)}{ending} ({typeof(TItem).Name})";
    }

    private static string NotEnumerated<TItem>(IAsyncEnumerable<TItem> _) => $"not enumerated ({typeof(TItem).Name})";

    private static string Names(IEnumerable<string> names) => names.Any() ? string.Join(", ", names) : "<none>";

    private static void Print(string label, string result) => Console.WriteLine($"{label}: {result}");
}

// The names that start with A, ordered by name: Albania, Andorra and Austria. dropAll adds a PostProcessingAction that
// drops every name, and select: false leaves the Select out.
public sealed class CountryNames : Specification<Country, string>
{
    public CountryNames(bool dropAll = false, bool select = true)
    {
        Query.Where(country => country.Name.StartsWith("A")).OrderBy(country => country.Name);
        if (select) Query.Select(country => country.Name);
        if (dropAll) Query.PostProcessingAction(names => Enumerable.Empty<string>());
    }
}
