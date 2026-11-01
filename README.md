# OpenSpecification

A .NET library for building query specifications.

OpenSpecification continues Ardalis.Specification under the MIT license. To move a project from Ardalis.Specification 9.x, follow the migration at the top of [CHANGELOG.md](CHANGELOG.md).

## Give a Star! :star:
If you like or are using this project, please give it a star. Thanks!

## Documentation

### [Read the Documentation](https://bandoyer.github.io/OpenSpecification/)

## Releases

[CHANGELOG.md](CHANGELOG.md) lists each version's changes and breaking changes.

## Sample Usage

The Specification pattern pulls query-specific logic out of other places in the application where it currently exists.
- For applications with minimal abstraction that use EF Core directly, the specification will eliminate `Where`, `Include`, `Select`, and similar expressions from almost all places where they're being used.
- In applications that abstract database query logic behind a `Repository` abstraction, the specification will typically eliminate the need for many custom `Repository` implementation classes as well as custom query methods on `Repository` implementations. Instead of many different ways to filter and shape data using various methods, the same capability is achieved with a few core methods.

Example implementation in your repository using specifications

```csharp
public async Task<List<T>> ListAsync(
    ISpecification<T> specification,
    CancellationToken cancellationToken = default)
{
    var query = SpecificationEvaluator.Default
        .GetQuery(_dbContext.Set<T>(), specification);

    return await query.ToListAsync(cancellationToken);
}
```

Now, to use this method, the calling code simply instantiates and passes the appropriate specification.

```csharp
var spec = new CustomerByNameSpec("customerName");
var customers = await _repository.ListAsync(spec, cancellationToken);
```
Specifications should be defined in an easily-discovered location in the application, so developers can easily reuse them. The use of this pattern helps to eliminate many commonly duplicated lambda expressions in applications, reducing bugs associated with this duplication.

We're shipping a built-in repository implementation [RepositoryBase](src/OpenSpecification.EntityFrameworkCore/RepositoryBaseOfT.cs), ready to be consumed in your apps. You can use it as a reference and create your own custom repository implementation.

## Reference

Please refer to the documentation for [related resources](docs/related-resources/).

Pluralsight resources:

- [Design Patterns Library - Specification](https://www.pluralsight.com/courses/patterns-library)
- [Domain-Driven Design Fundamentals](https://www.pluralsight.com/courses/domain-driven-design-fundamentals)
- [Specification Pattern in C#](https://www.pluralsight.com/courses/csharp-specification-pattern)
