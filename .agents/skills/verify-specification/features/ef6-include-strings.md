# EF6 include strings

A developer names the navigations a query loads as strings, such as `Include("Country")` or the dotted path `Include("Stores.Products")`, alone or beside include expressions. The run prints what each query loads, the SQL EF6 builds for it, and what a repository loads when its custom evaluator list holds only one of the include evaluators.

## Sub-features

Each ID is a label the consumer prints.

- `include-string`, `include-strings`, `include-string-and-expression`: one include string; two, one of them the dotted path `Stores.Products`; and one include string with one include expression.
- `sql-include-string`, `sql-include-strings`, `sql-include-string-and-expression`: the SQL EF6 builds for those three specifications, through `WithSpecification`, on one line.
- `include-string-invalid`: an include string that names no navigation, and the exception it throws.
- `include-evaluator-alone`, `include-string-evaluator-alone`: a repository given a custom evaluator list holding only `IncludeEvaluator`, or only `IncludeStringEvaluator`, with the specification of `include-string-and-expression`.

## How to get to it (user POV)

- Call `Include` with a string on a specification's builder, and list through `RepositoryBase<T>`.
- Call `WithSpecification` on a `DbSet`, and read the query's SQL with `ToString()`.
- Pass `RepositoryBase<T>` a `SpecificationEvaluator` built from your own evaluator list.

## Driving it with verify

Preconditions:

- `verify doctor` passes.
- `C` is a consumer from `verify consumer`.

- **Run.** Run `verify run "$C"`. It prints these nine labels right after `include-then-include`. Against main at `25b1a1e` they read as below, with each `sql-` line's SELECT cut short here:

  ```
  include-string: Acme (Albania), Globex (Norway), Initech (Norway)
  include-strings: Acme (Albania) [Acme Durres: Bucket; Acme Tirana: Anvil, Rope], Globex (Norway) [Globex Bergen: ; Globex Oslo: Desk, Lamp], Initech (Norway) [Initech Remote: Stapler]
  include-string-and-expression: Acme (Albania) [Acme Durres, Acme Tirana], Globex (Norway) [Globex Bergen, Globex Oslo], Initech (Norway) [Initech Remote]
  sql-include-string: SELECT [Extent1].[Id] AS [Id], ... FROM [dbo].[Companies] AS [Extent1] INNER JOIN [dbo].[Countries] AS [Extent2] ON [Extent1].[CountryId] = [Extent2].[Id]
  sql-include-strings: SELECT [Project1].[Id] AS [Id], ...
  sql-include-string-and-expression: SELECT [Project1].[Id] AS [Id], ...
  include-string-invalid: InvalidOperationException: A specified Include path is not valid. The EntityType 'CodeFirstNamespace.Company' does not declare a navigation property with the name 'Nope'.
  include-evaluator-alone: Acme (Albania) [Acme Durres, Acme Tirana], Globex (Norway) [Globex Bergen, Globex Oslo], Initech (Norway) [Initech Remote]
  include-string-evaluator-alone: no IncludeStringEvaluator
  ```

- **Compare two commits.** Diff two commits' runs as [ef6-repository-queries.md](./ef6-repository-queries.md) says. Equal `sql-` lines show that the two commits send the same SQL.

## Gotchas

- `IncludeStringEvaluator` arrived with #6, and `IncludeEvaluator` stopped applying include strings. Before #6, `include-evaluator-alone` loads countries too, and `include-string-evaluator-alone` prints `no IncludeStringEvaluator`. The consumer finds that type by name, so it builds against a tree from either side.
- A company's `Stores` starts as an empty list, so stores that weren't loaded print `[]`. Every seeded company has stores, so `[]` means not loaded.
- `Globex Bergen` has no products, so `include-strings` prints `Globex Bergen: ` with nothing after the colon.
