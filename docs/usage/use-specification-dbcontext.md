---
layout: default
title: How to use Specifications with a DbContext
parent: Usage
nav_order: 2
---

# How to use Specifications with a DbContext

Specifications can be applied to any `DbSet<>` or `IQueryable<>` source using the `WithSpecification` extension method. This extension method is defined in the `OpenSpecification.EntityFrameworkCore` and `OpenSpecification.EntityFramework6` plugin packages.

Let's assume we have the following setup.

```csharp
public class CustomerSpec : Specification<Customer>
{
    public CustomerSpec(int age)
    {
        Query.Where(x => x.Age > age)
             .Include(x => x.Addresses)
             .OrderBy(x => x.FirstName);
    }
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class SampleDbContext : DbContext
{
    public DbSet<Customer> Customers { get; set; }

    public SampleDbContext(DbContextOptions<SampleDbContext> options)
        : base(options)
    {
    }
}
```

Apply the specification to a given `DbSet` as follows.

```csharp
var spec = new CustomerSpec(20);

var customer = await dbContext.Customers
    .WithSpecification(spec)
    .ToListAsync();
```

The `WithSpecification` extension methods returns the `IQueryable<>` source, and you may continue building the state. Having said that, you may chain multiple specifications as follows.

```csharp
var spec1 = new CustomerByAgeSpec(20);
var spec2 = new CustomerByLastNameSpec("Smith");

var customer = await dbContext.Customers
    .WithSpecification(spec1)
    .WithSpecification(spec2)
    .ToListAsync();
```

## Reading from a DbSet

The same plugin packages add methods to `DbSet<>` that apply a specification and run the query in one call. Each applies the specification as the repository method of the same name does.

```csharp
var spec = new CustomerSpec(20);

List<Customer> customers = await dbContext.Customers.ToListAsync(spec);
IEnumerable<Customer> sequence = await dbContext.Customers.ToEnumerableAsync(spec);
Customer? first = await dbContext.Customers.FirstOrDefaultAsync(spec);
int count = await dbContext.Customers.CountAsync(spec);
bool any = await dbContext.Customers.AnyAsync(spec);

// EF Core only.
await foreach (var customer in dbContext.Customers.AsAsyncEnumerable(spec))
{
    Console.WriteLine(customer.Name);
}
```

`SingleOrDefaultAsync` takes a specification meant to return a single result.

```csharp
public class CustomerByIdSpec : SingleResultSpecification<Customer>
{
    public CustomerByIdSpec(int id)
    {
        Query.Where(x => x.Id == id);
    }
}

Customer? customer = await dbContext.Customers.SingleOrDefaultAsync(new CustomerByIdSpec(1));
```

| Method | Returns |
|---|---|
| `ToListAsync(spec)` | A `List<>` of the matches, after the specification's `PostProcessingAction`. |
| `ToEnumerableAsync(spec)` | The matches as an `IEnumerable<>`, after the specification's `PostProcessingAction`. |
| `FirstOrDefaultAsync(spec)` | The first match, or `null`. |
| `SingleOrDefaultAsync(spec)` | The only match, or `null`. It throws when more than one row matches, and takes an `ISingleResultSpecification<>`. |
| `CountAsync(spec)` | The number of matches. It evaluates the criteria only, so `Skip` and `Take` don't change the count. |
| `AnyAsync(spec)` | Whether any row matches. It evaluates the criteria only. |
| `AsAsyncEnumerable(spec)` | The matches as an `IAsyncEnumerable<>`. EF Core only: the EF6 package has no `AsAsyncEnumerable`. |

Each method except `AsAsyncEnumerable` takes a `CancellationToken` after the specification. To call one without a token, leave the argument out, or pass `CancellationToken.None`. With these methods in scope, `dbContext.Customers.FirstOrDefaultAsync(default)` passes `default` as the specification and throws `ArgumentNullException`, and so do `SingleOrDefaultAsync`, `CountAsync` and `AnyAsync`.

Given a specification with a `Select`, `ToListAsync`, `ToEnumerableAsync`, `FirstOrDefaultAsync`, `SingleOrDefaultAsync` and `AsAsyncEnumerable` return the projected type.

```csharp
public class CustomerNameSpec : Specification<Customer, string>
{
    public CustomerNameSpec(int age)
    {
        Query.Where(x => x.Age > age);
        Query.Select(x => x.Name);
    }
}

public class CustomerNameByIdSpec : SingleResultSpecification<Customer, string>
{
    public CustomerNameByIdSpec(int id)
    {
        Query.Where(x => x.Id == id);
        Query.Select(x => x.Name);
    }
}

var namesSpec = new CustomerNameSpec(20);

List<string> names = await dbContext.Customers.ToListAsync(namesSpec);
IEnumerable<string> nameSequence = await dbContext.Customers.ToEnumerableAsync(namesSpec);
string? firstName = await dbContext.Customers.FirstOrDefaultAsync(namesSpec);
string? name = await dbContext.Customers.SingleOrDefaultAsync(new CustomerNameByIdSpec(1));

// EF Core only.
await foreach (string customerName in dbContext.Customers.AsAsyncEnumerable(namesSpec))
{
    Console.WriteLine(customerName);
}
```
