using OpenSpecification;

namespace OpenSpecification.Sample.Domain.Specs;

public class AdultCustomersByNameSpec : Specification<Customer>
{
    public AdultCustomersByNameSpec(string nameSubstring)
    {
        Query.IsAdult()
            .NameIncludes(nameSubstring);
    }
}
