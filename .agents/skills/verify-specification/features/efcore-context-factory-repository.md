# EF Core context-factory repository

A developer registers an `IDbContextFactory<TContext>` and derives a repository from `ContextFactoryRepositoryBaseOfT<TEntity, TContext>`. Each call makes a context from the factory and disposes it when the call is done. `AsAsyncEnumerable` returns a sequence that the caller reads later with `await foreach`, so its context must stay open until that enumeration ends.

## Sub-features

Each ID is a label the EF Core consumer prints. A line lists the names the `await foreach` read, then `then break` or the exception's type when it stopped early. Each `factory-` line ends with how many contexts its factory made and how many of them were disposed.

- `repository-async-enumerable`, `repository-async-enumerable-cancel`: the control. `RepositoryBase<T>.AsAsyncEnumerable` runs over a context the caller owns, reading every row, then with a `WithCancellation` token cancelled after the first row.
- `factory-async-enumerable`: every row.
- `factory-async-enumerable-break`: a `break` after the first row.
- `factory-async-enumerable-cancel`: the `WithCancellation` token cancelled after the first row.
- `factory-async-enumerable-twice`: two passes over one returned sequence, with ` | ` between them.
- `factory-async-enumerable-unenumerated`: a sequence the caller never enumerates.

## How to get to it (user POV)

- Register a context factory, such as with `AddDbContextFactory<TContext>()`, and derive a repository from `ContextFactoryRepositoryBaseOfT<TEntity, TContext>`.
- Call `AsAsyncEnumerable(spec)` and read it with `await foreach`, with or without `.WithCancellation(token)`.

## Driving it with verify

Preconditions:

- `verify doctor` passes.
- `C` is a consumer from `verify efcore-consumer`.

- **Run.** Run `verify run "$C"`. It exits 0 and prints these lines first, in this order, then the lines of [DbSet read extensions](./dbset-read-extensions.md). With #34's fix they read:

  ```
  repository-async-enumerable: Albania, Andorra, Austria
  repository-async-enumerable-cancel: Albania then TaskCanceledException
  factory-async-enumerable: Albania, Andorra, Austria; 1 context made, 1 disposed
  factory-async-enumerable-break: Albania then break; 1 context made, 1 disposed
  factory-async-enumerable-cancel: Albania then TaskCanceledException; 1 context made, 1 disposed
  factory-async-enumerable-twice: Albania, Andorra, Austria | Albania, Andorra, Austria; 2 contexts made, 2 disposed
  factory-async-enumerable-unenumerated: not enumerated; 0 contexts made, 0 disposed
  ```

- **Compare two commits.** Make an EF Core consumer for each tree, save each run's output to its own file, and `diff` the files.

## Gotchas

- Main before #34's fix, at `2f2c918`, prints `<none> then ObjectDisposedException` on `factory-async-enumerable`, `-break`, `-cancel` and both passes of `-twice`. Its `-unenumerated` line reads `1 context made, 1 disposed`, because that code makes the context when the method is called.
- A `factory-async-enumerable-cancel` line that lists all three names means the repository dropped the caller's token. A `factory-` line with fewer contexts disposed than made means a context leaked.
- The cancel lines show `TaskCanceledException`, which SqlClient throws for EF Core. It derives from `OperationCanceledException`.
- This feature has no probes. For a consumer from a tree with #11's DbSet extensions, `verify build` prints the probe lines of [DbSet read extensions](./dbset-read-extensions.md).
- A run waits while the test suite holds the shared lock, and the first run of a consumer `verify build` hasn't built builds it first. A long pause before the first line is one of those.
