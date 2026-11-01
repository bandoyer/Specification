// Reads the Query builder through each specification interface. Since #8 took Query off both interfaces, each line
// marked QUERY fails with CS1061; before #8 the file builds. features/query-builder.md copies it into a consumer.
using OpenSpecification;

static class QueryThroughInterface
{
    static void Filter(ISpecification<Company> spec) => spec.Query.Where(c => c.Name == "Acme"); // QUERY
    static void Project(ISpecification<Company, string> spec) => spec.Query.Select(c => c.Name); // QUERY
}
