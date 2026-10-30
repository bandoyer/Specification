// Implements Query explicitly as a member of ISpecification<Company>. Since #8 took Query off the interface, the line
// marked QUERY fails with CS0539; before #8 the file builds. features/query-builder.md copies it into a consumer.
using Ardalis.Specification;

class ExplicitQuery : Specification<Company>, ISpecification<Company>
{
    ISpecificationBuilder<Company> ISpecification<Company>.Query => Query; // QUERY
}
