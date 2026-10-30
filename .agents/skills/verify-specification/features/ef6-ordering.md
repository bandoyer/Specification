# EF6 ordering

A developer orders a query through the EF6 provider with `OrderBy`, `OrderByDescending`, `ThenBy` and `ThenByDescending`, on a value-type key, a string key or a navigation's property. A spec that starts two order chains throws `DuplicateOrderChainException`. The provider's `OrderEvaluator` also has an in-memory `Evaluate`, which sorts a list.

## Sub-features

Each ID is a label the consumer prints, right after `order-descending-then-by` from [EF6 repository queries](./ef6-repository-queries.md).

- `order-by-value-key`, `order-by-descending-value-key`: a spec with only `OrderBy`, or only `OrderByDescending`, on the `decimal` key `Price`.
- `order-by-navigation-then-by-descending`: `OrderBy` on `Company.Name`, a navigation's property, then `ThenByDescending` on a string key.
- `order-duplicate-chain`: a spec that calls `OrderBy` twice. `ListAsync` throws, and the line shows the exception's type name.
- `order-in-memory`: the EF6 `OrderEvaluator.Instance.Evaluate` with `OrderByDescending` and `ThenBy`, on the products a plain `ListAsync` returned. The line shows the rows, or the exception's type name when `Evaluate` throws.

## How to get to it (user POV)

- Call `OrderBy` or `OrderByDescending` on a specification's builder, then any `ThenBy` or `ThenByDescending`, and list it through `RepositoryBase<T>` or the `DbSet` extensions.
- Call `Ardalis.Specification.EntityFramework6.OrderEvaluator.Instance.Evaluate` on an `IEnumerable<T>`.

## Driving it with verify

Preconditions:

- `verify doctor` passes.
- `C` is a consumer from `verify consumer`.

- **Run.** Run `verify run "$C"`. Against main at `f390273`, the five lines read:

  ```
  order-by-value-key: Rope, Bucket, Stapler, Anvil, Lamp, Desk
  order-by-descending-value-key: Desk, Lamp, Anvil, Stapler, Bucket, Rope
  order-by-navigation-then-by-descending: Acme Tirana, Acme Durres, Globex Oslo, Globex Bergen, Initech Remote
  order-duplicate-chain: DuplicateOrderChainException
  order-in-memory: Rope, Bucket, Anvil, Stapler, Lamp, Desk
  ```

- **Compare two commits.** Make a consumer for each tree and `diff` their outputs, as [EF6 repository queries](./ef6-repository-queries.md) says.

## Gotchas

- Before `4122aa6`, the change for #7, `Evaluate` throws `RuntimeBinderException` for every spec with an order, so `order-in-memory` shows that name.
- Each key in these orders is unique within its group, each price and each city within a company, so every order has one right answer and two runs compare line for line.
