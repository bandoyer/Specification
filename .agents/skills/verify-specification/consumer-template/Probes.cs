// Compile-only probes of the EF6 provider's nullable annotations, and of where a `default` or `null` argument to a
// DbSet extension binds, as a caller with nullable enabled meets them.
// Nothing calls these methods. `verify build` lists the warnings each PROBE line raises, beside the codes a provider
// annotated like the EF Core one gives; against a provider without annotations every probe gets none.
using Ardalis.Specification;
using Ardalis.Specification.EntityFramework6;

static class Probes
{
    static async Task DereferenceResults(Repository<Company> repo)
    {
        var byName = new SingleResultSpecification<Company>();
        var names = new SingleResultSpecification<Company, string>();
        _ = (await repo.GetByIdAsync(1)).Name; // PROBE get-by-id-dereference: CS8602
        _ = (await repo.FirstOrDefaultAsync(byName)).Name; // PROBE first-dereference: CS8602
        _ = (await repo.FirstOrDefaultAsync(names)).Length; // PROBE first-projected-dereference: CS8602
        _ = (await repo.SingleOrDefaultAsync(byName)).Name; // PROBE single-dereference: CS8602
        _ = (await repo.SingleOrDefaultAsync(names)).Length; // PROBE single-projected-dereference: CS8602
    }

    static void PassNullEvaluator(IQueryable<Company> companies)
    {
        _ = companies.WithSpecification(new Specification<Company>(), evaluator: null); // PROBE with-specification-null-evaluator: none
        _ = companies.WithSpecification(new Specification<Company, string>(), evaluator: null); // PROBE with-specification-projected-null-evaluator: none
    }

    static async Task PassNullArguments(Repository<Company> repo, IQueryable<Company> companies)
    {
        _ = await repo.ListAsync(null); // PROBE list-null-specification: CS8625
        _ = companies.WithSpecification(null); // PROBE with-specification-null-specification: CS8625
        _ = companies.WithSpecification<Company, string>(null); // PROBE with-specification-projected-null-specification: CS8625
        _ = await repo.GetByIdAsync<string?>("id"); // PROBE get-by-id-nullable-id-type: CS8714
    }
}

#if DBSET_READS
// #11's DbSet extensions. A `default` argument binds to their specification overload, whose parameter isn't nullable,
// so it warns; EF's CancellationToken overload wouldn't. A `null` warns in EF Core whichever overload takes it, since
// EF Core's predicate parameter isn't nullable either, so Program.cs's first-null lines show the overload instead.
static class DbSetProbes
{
    static async Task PassDefault(ShopContext db)
    {
        _ = await db.Companies.FirstOrDefaultAsync(default); // PROBE dbset-first-default: CS8625
        _ = await db.Companies.FirstOrDefaultAsync(null); // PROBE dbset-first-null: CS8625
    }
}

// The EF Core provider's extensions, imported here as a consumer imports them beside EF Core's own.
namespace EfCoreProbes
{
    using Ardalis.Specification.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;

    static class DbSetProbes
    {
        static async Task PassDefault(ShopCoreContext db)
        {
            _ = await db.Companies.FirstOrDefaultAsync(default); // PROBE efcore-dbset-first-default: CS8625
            _ = await db.Companies.FirstOrDefaultAsync(null); // PROBE efcore-dbset-first-null: CS8625
        }
    }
}
#endif
