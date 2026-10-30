// Compile-only probes of where a `default` or `null` argument to the EF Core provider's DbSet extensions binds, as a
// caller with nullable enabled meets them. Nothing calls these methods. `verify build` lists the warnings each PROBE
// line raises, beside the codes the line names.
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

#if DBSET_READS
// #11's DbSet extensions. A `default` argument binds to their specification overload, whose parameter isn't nullable,
// so it warns; EF Core's CancellationToken overload wouldn't. A `null` warns whichever overload takes it, since EF
// Core's predicate parameter isn't nullable either, so Shop.cs's efcore-dbset-first-null line shows the overload.
static class DbSetProbes
{
    static async Task PassDefault(AtlasContext db)
    {
        _ = await db.Companies.FirstOrDefaultAsync(default); // PROBE efcore-dbset-first-default: CS8625
        _ = await db.Companies.FirstOrDefaultAsync(null); // PROBE efcore-dbset-first-null: CS8625
    }
}
#endif
