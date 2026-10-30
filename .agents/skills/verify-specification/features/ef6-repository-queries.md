# EF6 repository queries

A developer queries SQL Server through the EF6 provider. A specification's filters, searches, includes, ordering, paging and projections become one query, read through `RepositoryBase<T>` or the `DbSet` extensions.

## Sub-features

Each ID is a label the consumer prints.

- `list-all`, `where`: every store, and the stores a `Where` keeps.
- `search`, `search-same-group`, `search-two-groups`: one `Search`; two in one group, where either may match; and two in separate groups, where both must match.
- `include-reference`, `include-then-include`: an `Include` of a reference, and an `Include` of a collection with one `ThenInclude`.
- `order-then-by-descending-paged`, `order-descending-then-by`: `OrderBy` with `ThenByDescending`, `Skip` and `Take`; and `OrderByDescending` with `ThenBy`.
- `count-ignores-paging`, `count-where`, `count-all`, `any-match`, `any-missing`: `CountAsync` and `AnyAsync`. A count ignores `Skip` and `Take`.
- `select`, `select-many`: a `Select` and a `SelectMany` projection.
- `first`, `first-missing`, `first-projected`, `first-projected-missing`, `single`, `single-missing`, `single-projected`, `single-projected-missing`, `get-by-id`, `get-by-id-missing`: `FirstOrDefaultAsync` and `SingleOrDefaultAsync`, each with and without a projection, and `GetByIdAsync`, for a row that exists and for one that doesn't.
- `post-processing`: a `PostProcessingAction` applied to the list.
- `tracking`, `as-no-tracking`: how many entities the context tracks after a list, without and with `AsNoTracking`.
- `dbset-to-list`, `dbset-to-enumerable`, `with-specification`, `with-specification-projected`: the `DbSet` extensions `ToListAsync` and `ToEnumerableAsync`, and `WithSpecification` with and without a projection.

## How to get to it (user POV)

- Derive a repository from `RepositoryBase<T>`, and call its read methods with a specification.
- Call `ToListAsync`, `ToEnumerableAsync` or `WithSpecification` on a `DbSet`.

## Driving it with verify

Preconditions:

- `verify doctor` passes.
- `C` is a consumer from `verify consumer`.

- **Run.** Run `verify run "$C"`. It exits 0 and prints one line per label. This feature's labels come in this order, with other features' lines among them. Against main at `f390273` they read:

  ```
  list-all: Acme Durres, Acme Tirana, Globex Bergen, Globex Oslo, Initech Remote
  where: Acme Durres, Acme Tirana
  search: Globex Bergen, Globex Oslo
  search-same-group: Acme Tirana, Globex Oslo
  search-two-groups: Globex Bergen
  include-reference: Acme (Albania), Globex (Norway), Initech (Norway)
  include-then-include: Acme [Acme Durres: Bucket; Acme Tirana: Anvil, Rope], Globex [Globex Bergen: ; Globex Oslo: Desk, Lamp], Initech [Initech Remote: Stapler]
  order-then-by-descending-paged: Lamp, Stapler, Anvil, Bucket
  order-descending-then-by: Anvil, Bucket, Rope, Stapler, Desk, Lamp
  count-ignores-paging: 6
  count-where: 4
  count-all: 6
  any-match: True
  any-missing: False
  select: Bergen, Durres, Oslo, Tirana
  select-many: Globex Bergen, Globex Oslo
  first: Globex
  first-missing: <null>
  first-projected: Norway
  first-projected-missing: <null>
  single: Initech
  single-missing: <null>
  single-projected: Albania
  single-projected-missing: <null>
  get-by-id: Acme
  get-by-id-missing: <null>
  post-processing: Globex Oslo, Globex Bergen
  tracking: 4 tracked
  as-no-tracking: 0 tracked
  dbset-to-list: Globex Oslo
  dbset-to-enumerable: Acme Durres
  with-specification: Initech Remote
  with-specification-projected: Acme, Globex, Initech
  ```

- **Compare two commits.** Make a consumer for each tree, save each run's output to its own file, and `diff` the files. Equal files, each line with its rows, show that the two commits query alike.

## Gotchas

- `include-then-include` has one chain. [EF6 include chains](./ef6-include-chains.md) drives a second.
- Each query opens a fresh context, so a navigation holds only what that query's own `Include` loaded. One shared context would fill navigations from earlier queries and hide a missing include.
- A run waits while the test suite holds the shared lock. A long pause before the first line is the lock.
- `Globex Bergen` has no products, so `include-then-include` prints `Globex Bergen: ` with nothing after the colon.
