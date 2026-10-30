// Compile-only probes of where a `default` or `null` argument to #11's DbSet extensions binds, as a caller with
// nullable enabled meets them. `verify consumer` copies this file only from a tree that has those extensions.
// Nothing calls these methods. `verify build` lists the warnings each PROBE line raises, beside the codes it names.
using System.Data.Entity;
using Ardalis.Specification.EntityFramework6;

// A `default` or `null` argument binds to the specification overload, whose parameter isn't nullable, so it warns.
// EF6's own overloads have no annotations, and wouldn't.
static class DbSetProbes
{
    static async Task PassDefault(ShopContext db)
    {
        _ = await db.Companies.FirstOrDefaultAsync(default); // PROBE dbset-first-default: CS8625
        _ = await db.Companies.FirstOrDefaultAsync(null); // PROBE dbset-first-null: CS8625
    }
}
