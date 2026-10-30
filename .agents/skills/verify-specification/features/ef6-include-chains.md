# EF6 include chains

A developer loads navigations two levels deep or more with `Include(...).ThenInclude(...)`, and can start a second chain with another `Include`. Each chain loads what it names, whichever chains come before it.

## Sub-features

Each ID is a label the consumer prints.

- `include-two-chains`: two chains from the same navigation, `Include(c => c.Stores).ThenInclude(s => s.Products)` and then `Include(c => c.Stores).ThenInclude(s => s.Company).ThenInclude(c => c.Country)`, listed through `RepositoryBase<T>`. The second chain reaches each company again and loads its `Country`, so its load shows in the output.

## How to get to it (user POV)

- Call `Include` and `ThenInclude` on a specification's builder more than once, and list through `RepositoryBase<T>`.

## Driving it with verify

Preconditions:

- `verify doctor` passes.
- `C` is a consumer from `verify consumer`.

- **Run.** Run `verify run "$C"`. It prints `include-two-chains` right after `include-then-include`. With #3's fix, each company shows its country and each store its products:

  ```
  include-two-chains: Acme (Albania) [Acme Durres: Bucket; Acme Tirana: Anvil, Rope], Globex (Norway) [Globex Bergen: ; Globex Oslo: Desk, Lamp], Initech (Norway) [Initech Remote: Stapler]
  ```

- **Compare two commits.** Diff two commits' runs as [ef6-repository-queries.md](./ef6-repository-queries.md) says.

## Gotchas

- Before #3's fix, a `ThenInclude` after the first include path continued the first path, and `include-two-chains` prints `InvalidOperationException: A specified Include path is not valid. The EntityType 'CodeFirstNamespace.Product' does not declare a navigation property with the name 'Company'.` The query catches the exception, so the rest of the run still prints.
- The same chains after a `Where` worked before the fix too: the bug read the previous path from EF6's internal span, which a query straight from a `DbSet` carries.
- `Globex Bergen` has no products, so it prints `Globex Bergen: ` with nothing after the colon.
