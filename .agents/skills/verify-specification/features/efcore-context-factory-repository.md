# EF Core context-factory repository

A developer registers an `IDbContextFactory<TContext>` and derives a repository from `ContextFactoryRepositoryBaseOfT<TEntity, TContext>`. Each call makes a context from the factory and disposes it when the call is done. `AsAsyncEnumerable` returns a sequence that the caller reads later with `await foreach`, so its context must stay open until that enumeration ends. `SingleOrDefaultAsync` returns the one match or `null`, and throws `InvalidOperationException` when two rows match, as `RepositoryBase<T>` does.

## Sub-features

Each ID is a label the EF Core consumer prints. An `async-enumerable` line lists the names the `await foreach` read, then `then break` or the exception's type when it stopped early. A `single` or `first` line shows the name the call returned, `<null>`, or the exception's type. Each `factory-` line ends with how many contexts its factory made and how many of them were disposed.

- `repository-async-enumerable`, `repository-async-enumerable-cancel`: the control. `RepositoryBase<T>.AsAsyncEnumerable` runs over a context the caller owns, reading every row, then with a `WithCancellation` token cancelled after the first row.
- `factory-async-enumerable`: every row.
- `factory-async-enumerable-break`: a `break` after the first row.
- `factory-async-enumerable-cancel`: the `WithCancellation` token cancelled after the first row.
- `factory-async-enumerable-twice`: two passes over one returned sequence, with ` | ` between them.
- `factory-async-enumerable-unenumerated`: a sequence the caller never enumerates.
- `factory-single`, `factory-single-missing`, `factory-single-two-matches`, `factory-single-cancel`: `SingleOrDefaultAsync` with a single-result specification, for one match, no match, two matches (Albania and Austria), and a token cancelled before the call.
- `factory-single-projected`, `factory-single-projected-missing`, `factory-single-projected-two-matches`, `factory-single-projected-cancel`: the same through the overload whose specification selects the name.
- `factory-first-two-matches`: `FirstOrDefaultAsync` with the same two matches, ordered by name.

## How to get to it (user POV)

- Register a context factory, such as with `AddDbContextFactory<TContext>()`, and derive a repository from `ContextFactoryRepositoryBaseOfT<TEntity, TContext>`.
- Call `AsAsyncEnumerable(spec)` and read it with `await foreach`, with or without `.WithCancellation(token)`.
- Call `SingleOrDefaultAsync(spec)` with a `SingleResultSpecification<T>` or a `SingleResultSpecification<T, TResult>`, or `FirstOrDefaultAsync(spec)`.

## Driving it with verify

Preconditions:

- `verify doctor` passes.
- `C` is a consumer from `verify efcore-consumer`.

- **Run.** Run `verify run "$C"`. It exits 0 and prints these lines first, in this order, then the lines of [DbSet read extensions](./dbset-read-extensions.md). With #34's and #39's fixes they read:

  ```
  repository-async-enumerable: Albania, Andorra, Austria
  repository-async-enumerable-cancel: Albania then TaskCanceledException
  factory-async-enumerable: Albania, Andorra, Austria; 1 context made, 1 disposed
  factory-async-enumerable-break: Albania then break; 1 context made, 1 disposed
  factory-async-enumerable-cancel: Albania then TaskCanceledException; 1 context made, 1 disposed
  factory-async-enumerable-twice: Albania, Andorra, Austria | Albania, Andorra, Austria; 2 contexts made, 2 disposed
  factory-async-enumerable-unenumerated: not enumerated; 0 contexts made, 0 disposed
  factory-single: Norway; 1 context made, 1 disposed
  factory-single-missing: <null>; 1 context made, 1 disposed
  factory-single-two-matches: InvalidOperationException; 1 context made, 1 disposed
  factory-single-cancel: TaskCanceledException; 1 context made, 1 disposed
  factory-single-projected: Norway; 1 context made, 1 disposed
  factory-single-projected-missing: <null>; 1 context made, 1 disposed
  factory-single-projected-two-matches: InvalidOperationException; 1 context made, 1 disposed
  factory-single-projected-cancel: TaskCanceledException; 1 context made, 1 disposed
  factory-first-two-matches: Albania; 1 context made, 1 disposed
  ```

- **Compare two commits.** Make an EF Core consumer for each tree, save each run's output to its own file, and `diff` the files.

## Gotchas

- Main before #34's fix, at `2f2c918`, prints `<none> then ObjectDisposedException` on `factory-async-enumerable`, `-break`, `-cancel` and both passes of `-twice`. Its `-unenumerated` line reads `1 context made, 1 disposed`, because that code makes the context when the method is called.
- Main before #39's fix, at `c569ae7`, prints `Austria; 1 context made, 1 disposed` on `factory-single-two-matches` and `factory-single-projected-two-matches`: both overloads called `FirstOrDefaultAsync`, which returns the first row SQL Server reads.
- A `factory-async-enumerable-cancel` line that lists all three names means the repository dropped the caller's token. A `factory-` line with fewer contexts disposed than made means a context leaked.
- The cancel lines show `TaskCanceledException`, which SqlClient throws for EF Core. It derives from `OperationCanceledException`.
- This feature has no probes. For a consumer from a tree with #11's DbSet extensions, `verify build` prints the probe lines of [DbSet read extensions](./dbset-read-extensions.md).
- A run waits while the test suite holds the shared lock, and the first run of a consumer `verify build` hasn't built builds it first. A long pause before the first line is one of those.
