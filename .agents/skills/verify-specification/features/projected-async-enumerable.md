# Projected AsAsyncEnumerable

A developer streams the results of a projection specification, a `Specification<T, TResult>` with a `Select`, with `AsAsyncEnumerable` and `await foreach`. In the EF Core provider the call works through `IReadRepositoryBase<T>` over `RepositoryBase<T>` and over `ContextFactoryRepositoryBaseOfT`, and on a `DbSet<T>`, and returns `IAsyncEnumerable<TResult>`. EF6 has no async streams, so its `RepositoryBase<T>` throws `NotImplementedException`, as its entity overload does. #16 added the overload. Before it, the same call bound to the entity overload, ignored the `Select`, and returned the entities.

## Sub-features

Each ID is a label a consumer prints. A read lists the names its `await foreach` read, then `then break` or the exception's type when it stopped early. A call that throws prints `<exception> when called`, with `for <parameter>` for an `ArgumentException`. Each read ends with the element type the call returned in parentheses: `(String)` for #16's overload, and `(Country)` or `(Store)` for the entity overload. Each `projected-factory-` line ends with how many contexts its factory made and how many of them were disposed.

The EF Core consumer streams `CountryNames`, the countries starting with A, ordered by name, with the name selected.

- `projected-repository`, `projected-repository-typed`, `projected-repository-cancel`: through `IReadRepositoryBase<Country>` over `RepositoryBase<T>`, every row, then every row read with `await foreach (string name in ...)`, then a `WithCancellation` token cancelled after the first row.
- `projected-repository-post-processing`: a `PostProcessingAction` that drops every name, which the stream ignores. The line ends with what `ListAsync` returns for the same specification.
- `projected-repository-no-select`, `projected-repository-null`: a `Specification<Country, string>` with no `Select`, and `AsAsyncEnumerable<string>(null!)`.
- `projected-factory`, `projected-factory-break`, `projected-factory-cancel`, `projected-factory-twice`, `projected-factory-unenumerated`: through `IReadRepositoryBase<Country>` over `ContextFactoryRepositoryBaseOfT`, every row, a `break` after the first row, a cancel after the first row, two passes over one returned sequence with ` | ` between them, and a sequence never enumerated.
- `projected-factory-post-processing`, `projected-factory-no-select`, `projected-factory-null`: the same three cases as the repository's.
- `projected-dbset`, `projected-dbset-cancel`, `projected-dbset-post-processing`, `projected-dbset-no-select`, `projected-dbset-null`: through `db.Countries.AsAsyncEnumerable`, the same cases, with `AsAsyncEnumerable<Country, string>(null!)` for the null.
- `async-enumerable`, `projected-async-enumerable`: the EF6 consumer's `RepositoryBase<Store>.AsAsyncEnumerable`, given a `Specification<Store>` and a `Specification<Store, string>`.

## How to get to it (user POV)

- Write a `Specification<T, TResult>` with a `Select`, and pass it to `AsAsyncEnumerable` on a repository, or on a `DbSet` with `OpenSpecification.EntityFrameworkCore` imported. Read the result with `await foreach`, with or without `.WithCancellation(token)`.

## Driving it with verify

Preconditions:

- `verify doctor` passes.
- `E` is a consumer from `verify efcore-consumer`, and `C` one from `verify consumer`. Each command looks for `AsAsyncEnumerable<TResult>` in the tree's `src/<library>/IReadRepositoryBase.cs`, where the library is the name `verify library` prints. When it finds it, the consumer's `PROJECTED_ASYNC_ENUMERABLE` lines build. The `projected-dbset` lines also need a tree with #11's DbSet extensions.

- **Build.** Run `verify build "$E"`. It ends `build: exit 0`, and its `warnings:` count is the same as from the merge base's tree.
- **Run.** Run `verify run "$E"`. It exits 0, and prints these lines last, after [DbSet read extensions](./dbset-read-extensions.md)'s. With #16's overload they read:

  ```
  projected-repository: Albania, Andorra, Austria (String)
  projected-repository-typed: Albania, Andorra, Austria
  projected-repository-cancel: Albania then TaskCanceledException (String)
  projected-repository-post-processing: Albania, Andorra, Austria (String); ListAsync: <none>
  projected-repository-no-select: SelectorNotFoundException when called (String)
  projected-repository-null: ArgumentNullException for specification when called (String)
  projected-factory: Albania, Andorra, Austria (String); 1 context made, 1 disposed
  projected-factory-break: Albania then break (String); 1 context made, 1 disposed
  projected-factory-cancel: Albania then TaskCanceledException (String); 1 context made, 1 disposed
  projected-factory-twice: Albania, Andorra, Austria (String) | Albania, Andorra, Austria (String); 2 contexts made, 2 disposed
  projected-factory-unenumerated: not enumerated (String); 0 contexts made, 0 disposed
  projected-factory-post-processing: Albania, Andorra, Austria (String); 1 context made, 1 disposed
  projected-factory-no-select: <none> then SelectorNotFoundException (String); 1 context made, 1 disposed
  projected-factory-null: ArgumentNullException for specification when called (String); 0 contexts made, 0 disposed
  projected-dbset: Albania, Andorra, Austria (String)
  projected-dbset-cancel: Albania then TaskCanceledException (String)
  projected-dbset-post-processing: Albania, Andorra, Austria (String)
  projected-dbset-no-select: SelectorNotFoundException when called (String)
  projected-dbset-null: ArgumentNullException for specification when called (String)
  ```

- **Run EF6.** Run `verify run "$C"`. It exits 0, and prints these two lines last. With #16's overload they read:

  ```
  async-enumerable: NotImplementedException when called (Store)
  projected-async-enumerable: NotImplementedException when called (String)
  ```

- **Compare with the merge base.** Make a tree and both consumers for `$(git merge-base origin/HEAD HEAD)` too, save each run's output to its own file, and `diff` the files. The merge base has no `-typed` or `-null` lines. Each other `projected-` line ends `(Country)` there in place of `(String)`. Each `-no-select` line reads the three names there, since the entity overload ignores the missing `Select`, and the EF6 consumer's `projected-async-enumerable` line ends `(Store)`. Every line that comes before this feature's reads the same on both trees.

## Gotchas

- A tree from before #16 builds every line written with `var` against the entity overload, so its lines print `(Country)` and the names a `Country` holds. The line's element type is the only thing that tells the two overloads apart when the names match.
- The `-cancel` lines show `TaskCanceledException`, which SqlClient throws for EF Core. It derives from `OperationCanceledException`.
- Through `RepositoryBase<T>` and the `DbSet`, a missing `Select` throws when the method is called, because the evaluator runs then. The context-factory repository runs the evaluator only once an enumeration has made its context, so there the first step of `await foreach` throws, and the line shows the context it made and disposed.
- A `projected-factory-` line with fewer contexts disposed than made means a context leaked. A `-cancel` line that lists all three names means the call dropped the caller's token.
