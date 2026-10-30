# Specification verification map

This folder lists what `verify` drives in the Specification library, one file per feature. Read this index first, then use the feature's file as the recipe.

## Baseline preconditions

- `verify doctor` passes.
- Each commit under test has a tree from `verify tree <commit>`, and each tree has a consumer from `verify consumer`.
- SQL Server is the spec-sql container from CONTRIBUTING.md, or the server `SPECIFICATION_TEST_SQL_SERVER` names.

## Driving conventions

- Drive a commit through its tree. The working copy's uncommitted edits never reach a tree.
- Compare a branch with `git merge-base origin/HEAD HEAD`, since `main` moves while a branch waits.
- Run the consumer through `verify run`, which holds the shared lock the test suite also takes.

## Proof and skip reporting

- A proof names the tree's commit and the command, and keeps the command's whole output.
- A comparison shows that both outputs hold rows before it calls them equal.
- Report a recipe you couldn't drive with its command and what stopped it. A different recipe can't stand in for it.

## Feature entry contract

Each feature file starts with an H1 title and a paragraph on what a developer using the library sees. Four H2s follow, in this order: `Sub-features`, `How to get to it (user POV)`, `Driving it with verify`, and `Gotchas`.

## Features

- [EF6 nullable annotations](./ef6-nullable-annotations.md): the warnings a nullable-enabled caller gets from the EF6 provider's public members, and the warnings the provider and its test project build with.
- [EF6 ordering](./ef6-ordering.md): ordering a query through the EF6 provider, a spec with two order chains, and the `OrderEvaluator`'s in-memory `Evaluate`.
- [EF6 repository queries](./ef6-repository-queries.md): querying SQL Server through the EF6 provider's `RepositoryBase<T>` and `DbSet` extensions.
- [DbSet read extensions](./dbset-read-extensions.md): `FirstOrDefaultAsync`, `SingleOrDefaultAsync`, `CountAsync`, `AnyAsync` and the projection overloads on a `DbSet`, in both providers, EF Core's `AsAsyncEnumerable`, and where a `default` or `null` argument binds.
- [EF6 include chains](./ef6-include-chains.md): two `Include` and `ThenInclude` chains in one specification, as a repository loads them.
- [EF6 include strings](./ef6-include-strings.md): navigations loaded by include strings, the SQL EF6 builds for them, and a custom evaluator list holding one include evaluator.

The map covers the EF Core provider only through its `DbSet` extensions. It doesn't cover the core library's in-memory evaluation, or the sample apps, yet. A run that changes one adds its file here.
