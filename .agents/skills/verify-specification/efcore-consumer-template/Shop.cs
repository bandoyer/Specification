// The EF Core consumer's shop: the EF6 consumer's companies, stores and products, in two of the countries Program.cs
// seeds. Its lines query the shop through the EF Core provider's DbSet extensions, and print as efcore-dbset-<name>,
// running the specification the EF6 consumer's dbset-<name> line runs.
using OpenSpecification;
using OpenSpecification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

static class Shop
{
    public static async Task Seed(AtlasContext db)
    {
        var albania = await db.Countries.SingleAsync(country => country.Name == "Albania");
        var norway = await db.Countries.SingleAsync(country => country.Name == "Norway");
        db.Companies.Add(new Company
        {
            Name = "Acme",
            Country = albania,
            Stores =
            [
                new Store { Name = "Acme Tirana", City = "Tirana", Products = [NewProduct("Anvil", "tools", 10), NewProduct("Rope", "tools", 3)] },
                new Store { Name = "Acme Durres", City = "Durres", Products = [NewProduct("Bucket", "tools", 5)] },
            ],
        });
        db.Companies.Add(new Company
        {
            Name = "Globex",
            Country = norway,
            Stores =
            [
                new Store { Name = "Globex Oslo", City = "Oslo", Products = [NewProduct("Lamp", "home", 20), NewProduct("Desk", "home", 150)] },
                new Store { Name = "Globex Bergen", City = "Bergen" },
            ],
        });
        db.Companies.Add(new Company
        {
            Name = "Initech",
            Country = norway,
            Stores = [new Store { Name = "Initech Remote", City = "Remote", Products = [NewProduct("Stapler", "office", 7)] }],
        });
        await db.SaveChangesAsync();
    }

    static Product NewProduct(string name, string category, decimal price) => new() { Name = name, Category = category, Price = price };

    // Each query gets a fresh context, as in the EF6 consumer.
    public static async Task Query(DbContextOptions<AtlasContext> options)
    {
        async Task Print(string label, Func<AtlasContext, Task<string>> query)
        {
            await using var db = new AtlasContext(options);
            Console.WriteLine($"{label}: {await query(db)}");
        }

        await Print("efcore-dbset-to-list", async db => Sorted((await db.Stores.ToListAsync(Reads.StoresIn("Oslo"))).Select(s => s.Name)));
        await Print("efcore-dbset-to-enumerable", async db => Sorted((await db.Stores.ToEnumerableAsync(Reads.StoresIn("Durres"))).Select(s => s.Name)));
#if DBSET_READS
        // #11's DbSet extensions. The ! on default and null keeps their warning to with-dbset-reads/DbSetProbes.cs, and
        // changes no binding.
        await Print("efcore-dbset-first", async db => Show((await db.Companies.FirstOrDefaultAsync(Reads.CompanyNamed("Globex")))?.Name));
        await Print("efcore-dbset-first-missing", async db => Show((await db.Companies.FirstOrDefaultAsync(Reads.CompanyNamed("Nope")))?.Name));
        await Print("efcore-dbset-first-projected", async db => Show(await db.Companies.FirstOrDefaultAsync(Reads.CountryOf("Initech"))));
        await Print("efcore-dbset-first-projected-missing", async db => Show(await db.Companies.FirstOrDefaultAsync(Reads.CountryOf("Nope"))));
        await Print("efcore-dbset-single", async db => Show((await db.Companies.SingleOrDefaultAsync(Reads.OnlyCompanyNamed("Initech")))?.Name));
        await Print("efcore-dbset-single-missing", async db => Show((await db.Companies.SingleOrDefaultAsync(Reads.OnlyCompanyNamed("Nope")))?.Name));
        await Print("efcore-dbset-single-two-matches", db => Reads.OrException(async () => Show((await db.Companies.SingleOrDefaultAsync(Reads.OnlyCompanyIn("Norway")))?.Name)));
        await Print("efcore-dbset-single-projected", async db => Show(await db.Companies.SingleOrDefaultAsync(Reads.OnlyCountryOf("Acme"))));
        await Print("efcore-dbset-single-projected-missing", async db => Show(await db.Companies.SingleOrDefaultAsync(Reads.OnlyCountryOf("Nope"))));
        await Print("efcore-dbset-count-ignores-paging", async db => (await db.Products.CountAsync(Reads.PricierThanSixPaged())).ToString());
        await Print("efcore-dbset-any-match", async db => (await db.Products.AnyAsync(Reads.ProductNamed("Lamp"))).ToString());
        await Print("efcore-dbset-any-missing", async db => (await db.Products.AnyAsync(Reads.ProductNamed("Nope"))).ToString());
        await Print("efcore-dbset-any-ignores-paging", async db => (await db.Products.AnyAsync(Reads.ProductNamed("Lamp", skip: 1))).ToString());
        await Print("efcore-dbset-to-list-projected", async db => string.Join(", ", await db.Stores.ToListAsync(Reads.GlobexCities())));
        await Print("efcore-dbset-to-list-projected-post-processing", async db => string.Join(", ", await db.Stores.ToListAsync(Reads.GlobexCities(reversed: true))));
        await Print("efcore-dbset-to-enumerable-projected", async db => string.Join(", ", await db.Stores.ToEnumerableAsync(Reads.GlobexCities())));
        await Print("efcore-dbset-to-list-no-selector", db => Reads.OrException(async () => string.Join(", ", await db.Stores.ToListAsync(new Specification<Store, string>()))));
        await Print("efcore-dbset-first-default", db => Reads.OrException(async () => Show((await db.Companies.FirstOrDefaultAsync(default!))?.Name)));
        await Print("efcore-dbset-first-null", db => Reads.OrException(async () => Show((await db.Companies.FirstOrDefaultAsync(null!))?.Name)));
        await Print("efcore-dbset-as-async-enumerable", async db =>
        {
            var names = new List<string>();
            await foreach (var store in db.Stores.AsAsyncEnumerable(Reads.StoresOf("Acme")))
            {
                names.Add(store.Name);
            }
            return Sorted(names);
        });
#endif
    }

    static string Sorted(IEnumerable<string> names) => string.Join(", ", names.OrderBy(n => n, StringComparer.Ordinal));

    static string Show(string? value) => value ?? "<null>";
}

public sealed class Company
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int CountryId { get; set; }
    public Country Country { get; set; } = null!;
    public List<Store> Stores { get; set; } = [];
}

public sealed class Store
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string City { get; set; }
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public List<Product> Products { get; set; } = [];
}

public sealed class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public decimal Price { get; set; }
    public int StoreId { get; set; }
}
