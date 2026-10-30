// The EF6 provider's consumer. It seeds a database of its own on SQL Server, queries it through RepositoryBase<T>
// and the DbSet extensions, prints one "label: result" line per query, and drops the database. `verify run` runs it.
// The output holds no ids or paths, so two trees' outputs compare with diff.
using System.Data.Entity;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Ardalis.Specification;
using Ardalis.Specification.EntityFramework6;

// Never the test fixtures' SpecificationTestDB_* databases.
const string Catalog = "VerifySpecificationConsumer";

var server = Environment.GetEnvironmentVariable("SPECIFICATION_TEST_SQL_SERVER");
if (string.IsNullOrEmpty(server))
{
    Console.Error.WriteLine("SPECIFICATION_TEST_SQL_SERVER is unset; verify run sets it.");
    return 2;
}
var connectionString = new SqlConnectionStringBuilder(server) { InitialCatalog = Catalog }.ToString();

Database.SetInitializer<ShopContext>(null);
using (var db = new ShopContext(connectionString))
{
    if (db.Database.Exists()) db.Database.Delete();
    db.Database.Create();
    Shop.Seed(db);
}

try
{
    await Shop.Query(connectionString);
}
finally
{
    SqlConnection.ClearAllPools();
    using var db = new ShopContext(connectionString);
    db.Database.Delete();
}
return 0;

static class Shop
{
    public static void Seed(ShopContext db)
    {
        var albania = new Country { Name = "Albania" };
        var norway = new Country { Name = "Norway" };
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
        db.SaveChanges();
    }

    static Product NewProduct(string name, string category, decimal price) => new() { Name = name, Category = category, Price = price };

    // Each query gets a fresh context, so a navigation holds only what that query's own Include loaded.
    public static async Task Query(string connectionString)
    {
        async Task Print<T>(string label, Func<Repository<T>, ShopContext, Task<string>> query) where T : class
        {
            using var db = new ShopContext(connectionString);
            Console.WriteLine($"{label}: {await query(new Repository<T>(db), db)}");
        }

        await Print<Store>("list-all", async (repo, _) => Sorted((await repo.ListAsync()).Select(s => s.Name)));
        await Print<Store>("where", async (repo, _) => Sorted((await repo.ListAsync(Spec<Store>(q => q.Where(s => s.Company.Name == "Acme")))).Select(s => s.Name)));
        await Print<Store>("search", async (repo, _) => Sorted((await repo.ListAsync(Spec<Store>(q => q.Search(s => s.Name, "Globex%")))).Select(s => s.Name)));
        await Print<Store>("search-same-group", async (repo, _) => Sorted((await repo.ListAsync(Spec<Store>(q => q.Search(s => s.City, "Tir%").Search(s => s.City, "Osl%")))).Select(s => s.Name)));
        await Print<Store>("search-two-groups", async (repo, _) => Sorted((await repo.ListAsync(Spec<Store>(q => q.Search(s => s.Name, "%e%", 1).Search(s => s.City, "B%", 2)))).Select(s => s.Name)));
        await Print<Company>("include-reference", async (repo, _) => Sorted((await repo.ListAsync(Spec<Company>(q => q.Include(c => c.Country)))).Select(c => $"{c.Name} ({c.Country?.Name ?? "not loaded"})")));
        await Print<Company>("include-then-include", async (repo, _) => Sorted((await repo.ListAsync(Spec<Company>(q => q.Include(c => c.Stores).ThenInclude(s => s.Products))))
            .Select(c => $"{c.Name} [{string.Join("; ", c.Stores.OrderBy(s => s.Name, StringComparer.Ordinal).Select(s => $"{s.Name}: {Sorted(s.Products.Select(p => p.Name))}"))}]")));
        // Two chains from one navigation (#3). The second, Stores.Company.Country, reaches each company again and loads
        // its Country. An exception prints as the result, so a tree from before #3's fix still prints every line.
        await Print<Company>("include-two-chains", async (repo, _) =>
        {
            try
            {
                return Sorted((await repo.ListAsync(Spec<Company>(q => q.Include(c => c.Stores).ThenInclude(s => s.Products).Include(c => c.Stores).ThenInclude(s => s.Company).ThenInclude(c => c.Country))))
                    .Select(c => $"{c.Name} ({c.Country?.Name ?? "not loaded"}) [{string.Join("; ", c.Stores.OrderBy(s => s.Name, StringComparer.Ordinal).Select(s => $"{s.Name}: {Sorted(s.Products.Select(p => p.Name))}"))}]"));
            }
            catch (Exception e)
            {
                return $"{e.GetType().Name}: {e.Message}";
            }
        });
        await Print<Company>("include-string", async (repo, _) => Sorted((await repo.ListAsync(Spec<Company>(q => q.Include("Country")))).Select(c => $"{c.Name} ({c.Country?.Name ?? "not loaded"})")));
        await Print<Company>("include-strings", async (repo, _) => Sorted((await repo.ListAsync(Spec<Company>(q => q.Include("Country").Include("Stores.Products"))))
            .Select(c => $"{c.Name} ({c.Country?.Name ?? "not loaded"}) [{string.Join("; ", c.Stores.OrderBy(s => s.Name, StringComparer.Ordinal).Select(s => $"{s.Name}: {Sorted(s.Products.Select(p => p.Name))}"))}]")));
        await Print<Company>("include-string-and-expression", async (repo, _) => CountriesAndStores(await repo.ListAsync(CountryAndStores())));
        await Print<Company>("sql-include-string", (_, db) => Task.FromResult(Sql(db.Companies.WithSpecification(Spec<Company>(q => q.Include("Country"))))));
        await Print<Company>("sql-include-strings", (_, db) => Task.FromResult(Sql(db.Companies.WithSpecification(Spec<Company>(q => q.Include("Country").Include("Stores.Products"))))));
        await Print<Company>("sql-include-string-and-expression", (_, db) => Task.FromResult(Sql(db.Companies.WithSpecification(CountryAndStores()))));
        await Print<Company>("include-string-invalid", async (repo, _) =>
        {
            try
            {
                return $"{(await repo.ListAsync(Spec<Company>(q => q.Include("Nope")))).Count} rows, no exception";
            }
            catch (Exception e)
            {
                return $"{e.GetType().Name}: {e.Message}";
            }
        });
        // A custom evaluator list with one include evaluator, as a caller of SpecificationEvaluator's public constructor builds it.
        await Print<Company>("include-evaluator-alone", async (_, db) =>
            CountriesAndStores(await new EvaluatorRepository<Company>(db, new SpecificationEvaluator([IncludeEvaluator.Instance])).ListAsync(CountryAndStores())));
        await Print<Company>("include-string-evaluator-alone", async (_, db) => FindIncludeStringEvaluator() is { } evaluator
            ? CountriesAndStores(await new EvaluatorRepository<Company>(db, new SpecificationEvaluator([evaluator])).ListAsync(CountryAndStores()))
            : "no IncludeStringEvaluator");
        await Print<Product>("order-then-by-descending-paged", async (repo, _) => string.Join(", ", (await repo.ListAsync(Spec<Product>(q => q.OrderBy(p => p.Category).ThenByDescending(p => p.Price).Skip(1).Take(4)))).Select(p => p.Name)));
        await Print<Product>("order-descending-then-by", async (repo, _) => string.Join(", ", (await repo.ListAsync(Spec<Product>(q => q.OrderByDescending(p => p.Category).ThenBy(p => p.Name)))).Select(p => p.Name)));
        await Print<Product>("order-by-value-key", async (repo, _) => string.Join(", ", (await repo.ListAsync(Spec<Product>(q => q.OrderBy(p => p.Price)))).Select(p => p.Name)));
        await Print<Product>("order-by-descending-value-key", async (repo, _) => string.Join(", ", (await repo.ListAsync(Spec<Product>(q => q.OrderByDescending(p => p.Price)))).Select(p => p.Name)));
        await Print<Store>("order-by-navigation-then-by-descending", async (repo, _) => string.Join(", ", (await repo.ListAsync(Spec<Store>(q => q.OrderBy(s => s.Company.Name).ThenByDescending(s => s.City)))).Select(s => s.Name)));
        // An exception prints as its type name alone, so the line compares across commits.
        await Print<Product>("order-duplicate-chain", async (repo, _) =>
        {
            try { return string.Join(", ", (await repo.ListAsync(Spec<Product>(q => { q.OrderBy(p => p.Name); q.OrderBy(p => p.Price); }))).Select(p => p.Name)); }
            catch (Exception ex) { return ex.GetType().Name; }
        });
        await Print<Product>("order-in-memory", async (repo, _) =>
        {
            var products = await repo.ListAsync();
            try { return string.Join(", ", Ardalis.Specification.EntityFramework6.OrderEvaluator.Instance.Evaluate(products, Spec<Product>(q => q.OrderByDescending(p => p.Category).ThenBy(p => p.Price))).Select(p => p.Name)); }
            catch (Exception ex) { return ex.GetType().Name; }
        });
        await Print<Product>("count-ignores-paging", async (repo, _) => (await repo.CountAsync(Spec<Product>(q => q.OrderBy(p => p.Name).Skip(1).Take(2)))).ToString());
        await Print<Product>("count-where", async (repo, _) => (await repo.CountAsync(Spec<Product>(q => q.Where(p => p.Price > 6)))).ToString());
        await Print<Product>("count-all", async (repo, _) => (await repo.CountAsync()).ToString());
        await Print<Product>("any-match", async (repo, _) => (await repo.AnyAsync(Spec<Product>(q => q.Where(p => p.Name == "Lamp")))).ToString());
        await Print<Product>("any-missing", async (repo, _) => (await repo.AnyAsync(Spec<Product>(q => q.Where(p => p.Name == "Nope")))).ToString());
        await Print<Store>("select", async (repo, _) => string.Join(", ", await repo.ListAsync(Spec<Store, string>(q => { q.Where(s => s.City != "Remote").OrderBy(s => s.City); q.Select(s => s.City); }))));
        await Print<Company>("select-many", async (repo, _) => Sorted(await repo.ListAsync(Spec<Company, string>(q => { q.Where(c => c.Name == "Globex"); q.SelectMany(c => c.Stores.Select(s => s.Name)); }))));
        await Print<Company>("first", async (repo, _) => Show((await repo.FirstOrDefaultAsync(Spec<Company>(q => q.Where(c => c.Name == "Globex"))))?.Name));
        await Print<Company>("first-missing", async (repo, _) => Show((await repo.FirstOrDefaultAsync(Spec<Company>(q => q.Where(c => c.Name == "Nope"))))?.Name));
        await Print<Company>("first-projected", async (repo, _) => Show(await repo.FirstOrDefaultAsync(Spec<Company, string>(q => { q.Where(c => c.Name == "Initech"); q.Select(c => c.Country.Name); }))));
        await Print<Company>("first-projected-missing", async (repo, _) => Show(await repo.FirstOrDefaultAsync(Spec<Company, string>(q => { q.Where(c => c.Name == "Nope"); q.Select(c => c.Country.Name); }))));
        await Print<Company>("single", async (repo, _) => Show((await repo.SingleOrDefaultAsync(Single<Company>(q => q.Where(c => c.Name == "Initech"))))?.Name));
        await Print<Company>("single-missing", async (repo, _) => Show((await repo.SingleOrDefaultAsync(Single<Company>(q => q.Where(c => c.Name == "Nope"))))?.Name));
        await Print<Company>("single-projected", async (repo, _) => Show(await repo.SingleOrDefaultAsync(Single<Company, string>(q => { q.Where(c => c.Name == "Acme"); q.Select(c => c.Country.Name); }))));
        await Print<Company>("single-projected-missing", async (repo, _) => Show(await repo.SingleOrDefaultAsync(Single<Company, string>(q => { q.Where(c => c.Name == "Nope"); q.Select(c => c.Country.Name); }))));
        await Print<Company>("get-by-id", async (repo, _) =>
        {
            var acme = await repo.FirstOrDefaultAsync(Spec<Company>(q => q.Where(c => c.Name == "Acme")));
            return acme is null ? "no Acme to look up" : Show((await repo.GetByIdAsync(acme.Id))?.Name);
        });
        await Print<Company>("get-by-id-missing", async (repo, _) => Show((await repo.GetByIdAsync(-1))?.Name));
        await Print<Store>("post-processing", async (repo, _) => string.Join(", ", (await repo.ListAsync(Spec<Store>(q => q.Where(s => s.Company.Name == "Globex").OrderBy(s => s.Name).PostProcessingAction(stores => stores.Reverse())))).Select(s => s.Name)));
        await Print<Store>("tracking", async (repo, db) => { await repo.ListAsync(Spec<Store>(q => q.Where(s => s.City != "Remote"))); return $"{db.ChangeTracker.Entries().Count()} tracked"; });
        await Print<Store>("as-no-tracking", async (repo, db) => { await repo.ListAsync(Spec<Store>(q => q.Where(s => s.City != "Remote").AsNoTracking())); return $"{db.ChangeTracker.Entries().Count()} tracked"; });
        await Print<Store>("dbset-to-list", async (_, db) => Sorted((await db.Stores.ToListAsync(Spec<Store>(q => q.Where(s => s.City == "Oslo")))).Select(s => s.Name)));
        await Print<Store>("dbset-to-enumerable", async (_, db) => Sorted((await db.Stores.ToEnumerableAsync(Spec<Store>(q => q.Where(s => s.City == "Durres")))).Select(s => s.Name)));
        await Print<Store>("with-specification", async (_, db) => Sorted(await db.Stores.WithSpecification(Spec<Store>(q => q.Where(s => s.Company.Name == "Initech"))).Select(s => s.Name).ToListAsync()));
        await Print<Company>("with-specification-projected", async (_, db) => string.Join(", ", await db.Companies.WithSpecification(Spec<Company, string>(q => { q.OrderBy(c => c.Name); q.Select(c => c.Name); })).ToListAsync()));
#if DBSET_READS
        // #11's DbSet extensions, with the specifications the EF Core consumer runs. The ! on default and null keeps
        // their warning to with-dbset-reads/DbSetProbes.cs, and changes no binding.
        await Print<Company>("dbset-first", async (_, db) => Show((await db.Companies.FirstOrDefaultAsync(Reads.CompanyNamed("Globex")))?.Name));
        await Print<Company>("dbset-first-missing", async (_, db) => Show((await db.Companies.FirstOrDefaultAsync(Reads.CompanyNamed("Nope")))?.Name));
        await Print<Company>("dbset-first-projected", async (_, db) => Show(await db.Companies.FirstOrDefaultAsync(Reads.CountryOf("Initech"))));
        await Print<Company>("dbset-first-projected-missing", async (_, db) => Show(await db.Companies.FirstOrDefaultAsync(Reads.CountryOf("Nope"))));
        await Print<Company>("dbset-single", async (_, db) => Show((await db.Companies.SingleOrDefaultAsync(Reads.OnlyCompanyNamed("Initech")))?.Name));
        await Print<Company>("dbset-single-missing", async (_, db) => Show((await db.Companies.SingleOrDefaultAsync(Reads.OnlyCompanyNamed("Nope")))?.Name));
        await Print<Company>("dbset-single-two-matches", (_, db) => Reads.OrException(async () => Show((await db.Companies.SingleOrDefaultAsync(Reads.OnlyCompanyIn("Norway")))?.Name)));
        await Print<Company>("dbset-single-projected", async (_, db) => Show(await db.Companies.SingleOrDefaultAsync(Reads.OnlyCountryOf("Acme"))));
        await Print<Company>("dbset-single-projected-missing", async (_, db) => Show(await db.Companies.SingleOrDefaultAsync(Reads.OnlyCountryOf("Nope"))));
        await Print<Product>("dbset-count-ignores-paging", async (_, db) => (await db.Products.CountAsync(Reads.PricierThanSixPaged())).ToString());
        await Print<Product>("dbset-any-match", async (_, db) => (await db.Products.AnyAsync(Reads.ProductNamed("Lamp"))).ToString());
        await Print<Product>("dbset-any-missing", async (_, db) => (await db.Products.AnyAsync(Reads.ProductNamed("Nope"))).ToString());
        await Print<Product>("dbset-any-ignores-paging", async (_, db) => (await db.Products.AnyAsync(Reads.ProductNamed("Lamp", skip: 1))).ToString());
        await Print<Store>("dbset-to-list-projected", async (_, db) => string.Join(", ", await db.Stores.ToListAsync(Reads.GlobexCities())));
        await Print<Store>("dbset-to-list-projected-post-processing", async (_, db) => string.Join(", ", await db.Stores.ToListAsync(Reads.GlobexCities(reversed: true))));
        await Print<Store>("dbset-to-enumerable-projected", async (_, db) => string.Join(", ", await db.Stores.ToEnumerableAsync(Reads.GlobexCities())));
        await Print<Store>("dbset-to-list-no-selector", (_, db) => Reads.OrException(async () => string.Join(", ", await db.Stores.ToListAsync(new Specification<Store, string>()))));
        await Print<Company>("dbset-first-default", (_, db) => Reads.OrException(async () => Show((await db.Companies.FirstOrDefaultAsync(default!))?.Name)));
        await Print<Company>("dbset-first-null", (_, db) => Reads.OrException(async () => Show((await db.Companies.FirstOrDefaultAsync(null!))?.Name)));
#endif
    }

    static Specification<T> Spec<T>(Action<ISpecificationBuilder<T>> build)
    {
        var spec = new Specification<T>();
        build(spec.Query);
        return spec;
    }

    static Specification<T, TResult> Spec<T, TResult>(Action<ISpecificationBuilder<T, TResult>> build)
    {
        var spec = new Specification<T, TResult>();
        build(spec.Query);
        return spec;
    }

    static SingleResultSpecification<T> Single<T>(Action<ISpecificationBuilder<T>> build)
    {
        var spec = new SingleResultSpecification<T>();
        build(spec.Query);
        return spec;
    }

    static SingleResultSpecification<T, TResult> Single<T, TResult>(Action<ISpecificationBuilder<T, TResult>> build)
    {
        var spec = new SingleResultSpecification<T, TResult>();
        build(spec.Query);
        return spec;
    }

    static Specification<Company> CountryAndStores() => Spec<Company>(q => q.Include("Country").Include(c => c.Stores));

    static string CountriesAndStores(IEnumerable<Company> companies) =>
        Sorted(companies.Select(c => $"{c.Name} ({c.Country?.Name ?? "not loaded"}) [{Sorted(c.Stores.Select(s => s.Name))}]"));

    // The SQL EF6 builds, on one line.
    static string Sql<T>(IQueryable<T> query) => Regex.Replace(query.ToString() ?? "", @"\s+", " ").Trim();

    // By name, so the consumer still builds against a tree from before IncludeStringEvaluator existed (#6).
    static IEvaluator? FindIncludeStringEvaluator() =>
        typeof(IncludeEvaluator).Assembly.GetType("Ardalis.Specification.EntityFramework6.IncludeStringEvaluator")
            ?.GetProperty("Instance")?.GetValue(null) as IEvaluator;

    static string Sorted(IEnumerable<string> names) => string.Join(", ", names.OrderBy(n => n, StringComparer.Ordinal));

    static string Show(string? value) => value ?? "<null>";
}

public class Repository<T>(DbContext dbContext) : RepositoryBase<T>(dbContext) where T : class;

public class EvaluatorRepository<T>(DbContext dbContext, ISpecificationEvaluator evaluator) : RepositoryBase<T>(dbContext, evaluator) where T : class;

public class ShopContext(string connectionString) : DbContext(connectionString)
{
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Product> Products => Set<Product>();
}

public class Country
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int CountryId { get; set; }
    public Country Country { get; set; } = null!;
    public List<Store> Stores { get; set; } = [];
}

public class Store
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string City { get; set; } = "";
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public List<Product> Products { get; set; } = [];
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public int StoreId { get; set; }
}
