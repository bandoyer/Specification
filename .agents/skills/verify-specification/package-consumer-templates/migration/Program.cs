using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

var options = new DbContextOptionsBuilder<Shop>().UseInMemoryDatabase("migration").Options;
await using var db = new Shop(options);
db.Countries.AddRange(new Country { Id = 1, Name = "Norway" }, new Country { Id = 2, Name = "Austria" }, new Country { Id = 3, Name = "Albania" });
await db.SaveChangesAsync();
var names = (await new Repository(db).ListAsync(new StartingWithA())).Select(c => c.Name);
Console.WriteLine($"rows: {string.Join(", ", names)}");
Console.WriteLine($"library: {typeof(Specification<>).Assembly.GetName().Name} {typeof(Specification<>).Assembly.GetName().Version}");

public class Country { public int Id { get; set; } public string Name { get; set; } = ""; }
public class Shop(DbContextOptions<Shop> options) : DbContext(options) { public DbSet<Country> Countries => Set<Country>(); }
public class Repository(Shop db) : RepositoryBase<Country>(db);
public class StartingWithA : Specification<Country>
{
    public StartingWithA() => Query.Where(c => c.Name.StartsWith("A")).OrderBy(c => c.Name);
}
