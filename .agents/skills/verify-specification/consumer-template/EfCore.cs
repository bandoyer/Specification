// The consumer's EF Core half. It queries the tables Program.cs seeded through the EF Core provider's DbSet
// extensions, and prints each line as efcore-dbset-<name>, running the specification its dbset-<name> line runs.
using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

static class EfCoreShop
{
    // Each query gets a fresh context, as in Program.cs.
    public static async Task Query(string connectionString)
    {
        async Task Print(string label, Func<ShopCoreContext, Task<string>> query)
        {
            await using var db = new ShopCoreContext(connectionString);
            Console.WriteLine($"{label}: {await query(db)}");
        }

        await Print("efcore-dbset-to-list", async db => Shop.Sorted((await db.Stores.ToListAsync(Shop.Spec<Store>(q => q.Where(s => s.City == "Oslo")))).Select(s => s.Name)));
        await Print("efcore-dbset-to-enumerable", async db => Shop.Sorted((await db.Stores.ToEnumerableAsync(Shop.Spec<Store>(q => q.Where(s => s.City == "Durres")))).Select(s => s.Name)));
#if DBSET_READS
        // The ! on default and null keeps their warning to Probes.cs, and changes no binding.
        await Print("efcore-dbset-first", async db => Shop.Show((await db.Companies.FirstOrDefaultAsync(Reads.CompanyNamed("Globex")))?.Name));
        await Print("efcore-dbset-first-missing", async db => Shop.Show((await db.Companies.FirstOrDefaultAsync(Reads.CompanyNamed("Nope")))?.Name));
        await Print("efcore-dbset-first-projected", async db => Shop.Show(await db.Companies.FirstOrDefaultAsync(Reads.CountryOf("Initech"))));
        await Print("efcore-dbset-first-projected-missing", async db => Shop.Show(await db.Companies.FirstOrDefaultAsync(Reads.CountryOf("Nope"))));
        await Print("efcore-dbset-single", async db => Shop.Show((await db.Companies.SingleOrDefaultAsync(Reads.OnlyCompanyNamed("Initech")))?.Name));
        await Print("efcore-dbset-single-missing", async db => Shop.Show((await db.Companies.SingleOrDefaultAsync(Reads.OnlyCompanyNamed("Nope")))?.Name));
        await Print("efcore-dbset-single-two-matches", db => Reads.OrException(async () => Shop.Show((await db.Companies.SingleOrDefaultAsync(Reads.OnlyCompanyIn("Norway")))?.Name)));
        await Print("efcore-dbset-single-projected", async db => Shop.Show(await db.Companies.SingleOrDefaultAsync(Reads.OnlyCountryOf("Acme"))));
        await Print("efcore-dbset-single-projected-missing", async db => Shop.Show(await db.Companies.SingleOrDefaultAsync(Reads.OnlyCountryOf("Nope"))));
        await Print("efcore-dbset-count-ignores-paging", async db => (await db.Products.CountAsync(Reads.PricierThanSixPaged())).ToString());
        await Print("efcore-dbset-any-match", async db => (await db.Products.AnyAsync(Reads.ProductNamed("Lamp"))).ToString());
        await Print("efcore-dbset-any-missing", async db => (await db.Products.AnyAsync(Reads.ProductNamed("Nope"))).ToString());
        await Print("efcore-dbset-any-ignores-paging", async db => (await db.Products.AnyAsync(Reads.ProductNamed("Lamp", skip: 1))).ToString());
        await Print("efcore-dbset-to-list-projected", async db => string.Join(", ", await db.Stores.ToListAsync(Reads.GlobexCities())));
        await Print("efcore-dbset-to-list-projected-post-processing", async db => string.Join(", ", await db.Stores.ToListAsync(Reads.GlobexCities(reversed: true))));
        await Print("efcore-dbset-to-enumerable-projected", async db => string.Join(", ", await db.Stores.ToEnumerableAsync(Reads.GlobexCities())));
        await Print("efcore-dbset-to-list-no-selector", db => Reads.OrException(async () => string.Join(", ", await db.Stores.ToListAsync(new Specification<Store, string>()))));
        await Print("efcore-dbset-first-default", db => Reads.OrException(async () => Shop.Show((await db.Companies.FirstOrDefaultAsync(default!))?.Name)));
        await Print("efcore-dbset-first-null", db => Reads.OrException(async () => Shop.Show((await db.Companies.FirstOrDefaultAsync(null!))?.Name)));
        await Print("efcore-dbset-as-async-enumerable", async db =>
        {
            var names = new List<string>();
            await foreach (var store in db.Stores.AsAsyncEnumerable(Reads.StoresOf("Acme")))
            {
                names.Add(store.Name);
            }
            return Shop.Sorted(names);
        });
#endif
    }
}

// The same entities and tables as Program.cs's ShopContext, which creates them.
public class ShopCoreContext(string connectionString) : DbContext
{
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlServer(connectionString);

    // EF6 made the column decimal(18,2).
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
}
