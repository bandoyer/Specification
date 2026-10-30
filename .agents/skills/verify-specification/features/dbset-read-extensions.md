# DbSet read extensions

A developer reads through a specification straight from a `DbSet`, in either provider, without a repository: `FirstOrDefaultAsync`, `SingleOrDefaultAsync`, `CountAsync` and `AnyAsync`, the projection overloads of the first two and of `ToListAsync` and `ToEnumerableAsync`, and, in EF Core only, `AsAsyncEnumerable`. #11 added them. Each applies the specification as the repository's method of the same name does. A `default` or `null` passed as the only argument binds to the specification overload, which throws `ArgumentNullException`.

## Sub-features

Each ID is a label a consumer prints, as `dbset-<ID>` from the EF6 consumer and `efcore-dbset-<ID>` from the EF Core consumer. Each consumer seeds a shop of its own with the same rows, and runs the same specifications.

- `first`, `first-missing`, `first-projected`, `first-projected-missing`: `FirstOrDefaultAsync`, with and without a projection, for a row that exists and for one that doesn't.
- `single`, `single-missing`, `single-two-matches`, `single-projected`, `single-projected-missing`: `SingleOrDefaultAsync` with a single-result specification, the same way, and a specification two rows match.
- `count-ignores-paging`, `any-match`, `any-missing`, `any-ignores-paging`: `CountAsync` and `AnyAsync`, which evaluate the criteria only, so `Skip` and `Take` change nothing.
- `to-list-projected`, `to-list-projected-post-processing`, `to-enumerable-projected`, `to-list-no-selector`: `ToListAsync` and `ToEnumerableAsync` given a projection specification, with and without a `PostProcessingAction`, and given a `Specification<Store, string>` with no `Select`.
- `first-default`, `first-null`: `FirstOrDefaultAsync(default)` and `FirstOrDefaultAsync(null)`, printed as the exception and the parameter it names. The probes of the same names show the warning a nullable-enabled caller gets.
- `as-async-enumerable`: EF Core only, `AsAsyncEnumerable` read with `await foreach`.
- `to-list`, `to-enumerable`: EF Core only, the `ToListAsync` and `ToEnumerableAsync` that came before #11, as the EF6 lines `dbset-to-list` and `dbset-to-enumerable` in [EF6 repository queries](./ef6-repository-queries.md) call them.

## How to get to it (user POV)

- Import `Ardalis.Specification.EntityFramework6` beside `System.Data.Entity`, or `Ardalis.Specification.EntityFrameworkCore` beside `Microsoft.EntityFrameworkCore`, and call the methods on a context's `DbSet` with a specification.

## Driving it with verify

Preconditions:

- `verify doctor` passes.
- `C` is a consumer from `verify consumer`, and `E` one from `verify efcore-consumer`, both made from a tree that has #11's methods. Each command looks for `FirstOrDefaultAsync` in its provider's `DbSetExtensions.cs` in the tree. When it finds it, the consumer's `DBSET_READS` lines build.

- **Run.** Run `verify run "$C" > ef6.txt` and `verify run "$E" > efcore.txt`. Each exits 0. The EF6 lines follow [EF6 repository queries](./ef6-repository-queries.md)'s `with-specification-projected`, and the EF Core lines follow [EF Core context-factory repository](./efcore-context-factory-repository.md)'s seven lines. At 84ff252, where #11's branch merged main with #37, they read:

  ```
  dbset-first: Globex
  dbset-first-missing: <null>
  dbset-first-projected: Norway
  dbset-first-projected-missing: <null>
  dbset-single: Initech
  dbset-single-missing: <null>
  dbset-single-two-matches: InvalidOperationException
  dbset-single-projected: Albania
  dbset-single-projected-missing: <null>
  dbset-count-ignores-paging: 4
  dbset-any-match: True
  dbset-any-missing: False
  dbset-any-ignores-paging: True
  dbset-to-list-projected: Bergen, Oslo
  dbset-to-list-projected-post-processing: Oslo, Bergen
  dbset-to-enumerable-projected: Bergen, Oslo
  dbset-to-list-no-selector: SelectorNotFoundException
  dbset-first-default: ArgumentNullException (specification)
  dbset-first-null: ArgumentNullException (specification)
  ```

  ```
  efcore-dbset-to-list: Globex Oslo
  efcore-dbset-to-enumerable: Acme Durres
  efcore-dbset-first: Globex
  ...
  efcore-dbset-first-null: ArgumentNullException (specification)
  efcore-dbset-as-async-enumerable: Acme Durres, Acme Tirana
  ```

  The `efcore-dbset-*` lines between `first` and `first-null` read as the EF6 lines do.
- **Compare the providers.** Run `diff <(sed -n 's/^dbset-//p' ef6.txt) <(sed -n 's/^efcore-dbset-//p' efcore.txt)`. It prints only `> as-async-enumerable: Acme Durres, Acme Tirana`, since EF6 has no `AsAsyncEnumerable`. Before calling the rest equal, check that each run printed each line above with its rows.
- **Probes.** Run `tests/probes.sh`. It builds both consumers from HEAD and from main before #11, at 9fb6d84, first with LF probe files and then with CRLF ones, and exits 0. The EF Core consumer from 9fb6d84 has no probe file. Its `consumer with:` lines show `dbset-first-default` and `dbset-first-null` each getting CS8625, and its `efcore-consumer with:` lines show `efcore-dbset-first-default` and `efcore-dbset-first-null` each getting CS8625. Its `ok:` lines say the builds without the extensions printed no DbSet probe, and that CRLF changed no probe line.

## Gotchas

- A consumer from a tree before #11 builds without the `DBSET_READS` lines, and without the template's `with-dbset-reads/` probe file, so its build prints none of these probes. Of this feature, the EF6 consumer's run then prints no line, and the EF Core consumer's prints only `efcore-dbset-to-list` and `efcore-dbset-to-enumerable`.
- The EF6 consumer seeds its shop in `Program.cs`, and the EF Core consumer in `Shop.cs`, beside its countries. A change to the shop's rows or entities goes in both. Both consumers run the specifications in `consumer-shared/DbSetReads.cs`.
- In EF Core, a `null` warns CS8625 whichever overload takes it, since EF Core's predicate parameter isn't nullable either. The `first-null` line's parameter tells them apart: `specification` is #11's overload, and `predicate` would be EF's. A `default` warns only when the specification overload takes it.
- `Program.cs` and `Shop.cs` pass `default!` and `null!`, so their warnings stay on the probe lines. The `!` changes no binding.
