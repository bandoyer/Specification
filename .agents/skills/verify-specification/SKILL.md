---
name: verify-specification
description: Drives the Specification library's EF6 provider as a consuming project does. It builds a nullable-enabled console consumer against any commit and runs its queries on the spec-sql SQL Server. Use it to verify, smoke-test or compare a change to src/Ardalis.Specification.EntityFramework6.
---

# Verify the Specification library

The library has no app of its own. Its user is a developer whose project references a provider package, so this skill stands in for that project with a **consumer**: a net9.0 console project with nullable enabled, built against one commit's EF6 provider. Its build shows the warnings a caller gets. Its run queries SQL Server through `RepositoryBase<T>` and the `DbSet` extensions. [features/README.md](features/README.md) maps what each recipe drives.

Every step goes through one helper, `.agents/skills/verify-specification/bin/verify`, called `verify` below; `verify help` lists its commands. In a skillflow run, prefix each call with skillflow's `bin/sandbox`.

## Launch

1. Export each commit you drive as a **tree**: `T=$(verify tree <commit>)`. It runs `git archive`, so the tree holds exactly that commit, without uncommitted edits, and the checkout stays as it was. A tree is reused while it exists.
2. Make a consumer from the tree: `C=$(verify consumer "$T" <name>)`. The name defaults to the tree's short sha, and making a name again replaces that consumer.

The consumer is ready when `verify build "$C"` ends `build: exit 0`. Its run creates and seeds its own database, `VerifySpecificationConsumer`, so nothing needs seeding first.

## Doctor

`verify doctor` checks without writing. It looks for a .NET SDK 9 or later, the repository and its HEAD, and a listener on SQL Server's port. The server comes from `SPECIFICATION_TEST_SQL_SERVER`, or else CONTRIBUTING.md's spec-sql connection string. It also checks the shared lock's folder and the scratch root, and lists each tree and consumer with the commit it came from. It exits 1 on any `fail:` line. The port check doesn't log in; the first `verify run` does.

## Drive

- `verify build "$C"` builds the consumer, and the provider it references, for net9.0. It prints `warning <file>:<line> <code> <message>` once for each distinct warning, then `warnings: <n>`, then one `probe` line for each probe in `Probes.cs`, then `build: exit <n>`. A probe line reads `got <codes>; an annotated provider gives <codes>`.
- `verify run "$C"` runs the consumer against SQL Server while it holds `~/.cache/specification-spec-sql.lock`, the lock AGENTS.md puts around every test run. It prints one `label: result` line per query, then drops its database. Runs take turns on the lock with each other and with the test suite, so two never share the database at once.
- `verify warnings "$T/<path>.csproj" [-p:Nullable=<value>]` builds any project in a tree for every target framework, with `--no-incremental`. It prints each distinct warning with the frameworks it appeared in, NuGet's and MSBuild's included. A warning without a source position shows its project, or `MSBUILD`, in place of `<file>:<line>`.
- To compare a branch with where it started, drive two trees, `$(git merge-base origin/HEAD HEAD)` and `HEAD`, make a consumer for each, and `diff` their outputs.

Each feature file gives the commands and the observable result for its sub-features.

## Evidence

Save each command's stdout, with the command and the tree's commit, in the evidence folder your task names. In a skillflow run, that is `RUN/artifacts/<step>/`. The helper writes only inside its scratch root, so cleanup never reaches the evidence.

A comparison keeps both outputs and their diff. Before it calls two outputs equal, it shows that each holds the rows the criterion is about, since two empty outputs are equal.

## Cleanup

`verify cleanup <item>...` removes the trees and consumers it names. `verify cleanup` alone removes all of them, then the scratch root. Either way, it removes only a folder directly under the scratch root's `trees/` or `consumers/` that holds the `.verify-made` marker, after it resolves `..` and links, and it refuses anything else. Before it makes a folder, the helper also removes its own items older than a day. The run drops its database when it ends. A run killed midway leaves `VerifySpecificationConsumer` behind, and the next run drops it first.

## Helpers

- `bin/verify` is the CLI above. Its scratch root is `/tmp/verify-specification-<uid>`, with mode 700. The repository's `.gitignore` ignores every `bin/` folder, so a new file there needs `git add -f`.
- `consumer-template/` holds the consumer. `Program.cs` has the queries and `Probes.cs` the compile-only nullable probes. A run that needs another query or probe adds it there, and adds its sub-feature to the feature map.
- `tests/guards.sh` locks the guards on cleanup and on each command that writes. `tests/warnings.sh` checks that the warning report reads every form MSBuild and NuGet write. The `[pr]` suite in `skillflow.toml` runs every script in `tests/`. Run them after any change to `bin/verify`.
