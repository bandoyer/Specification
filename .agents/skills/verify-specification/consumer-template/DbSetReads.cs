// The specifications the DbSet extension lines run. Program.cs and EfCore.cs share them, so each efcore-dbset-<name>
// line compares with its dbset-<name> line.
using Ardalis.Specification;

static class Reads
{
    public static Specification<Company> CompanyNamed(string name) => Shop.Spec<Company>(q => q.Where(c => c.Name == name));

    public static Specification<Company, string> CountryOf(string company) =>
        Shop.Spec<Company, string>(q => { q.Where(c => c.Name == company); q.Select(c => c.Country.Name); });

    public static SingleResultSpecification<Company> OnlyCompanyNamed(string name) => Shop.Single<Company>(q => q.Where(c => c.Name == name));

    public static SingleResultSpecification<Company> OnlyCompanyIn(string country) => Shop.Single<Company>(q => q.Where(c => c.Country.Name == country));

    public static SingleResultSpecification<Company, string> OnlyCountryOf(string company) =>
        Shop.Single<Company, string>(q => { q.Where(c => c.Name == company); q.Select(c => c.Country.Name); });

    // Four of the six products cost more than 6. The paging would leave two of them.
    public static Specification<Product> PricierThanSixPaged() => Shop.Spec<Product>(q => q.Where(p => p.Price > 6).OrderBy(p => p.Name).Skip(1).Take(2));

    public static Specification<Product> ProductNamed(string name, int skip = 0) => Shop.Spec<Product>(q => q.Where(p => p.Name == name).Skip(skip, skip > 0));

    public static Specification<Store, string> GlobexCities(bool reversed = false) => Shop.Spec<Store, string>(q =>
    {
        q.Where(s => s.Company.Name == "Globex").OrderBy(s => s.City);
        q.Select(s => s.City);
        if (reversed) q.PostProcessingAction(cities => cities.Reverse());
    });

    public static Specification<Store> StoresOf(string company) => Shop.Spec<Store>(q => q.Where(s => s.Company.Name == company));

    // The query's result, or the type of the exception it threw, with the parameter's name for an argument exception.
    public static async Task<string> OrException(Func<Task<string>> query)
    {
        try
        {
            return await query();
        }
        catch (ArgumentException e)
        {
            return $"{e.GetType().Name} ({e.ParamName})";
        }
        catch (Exception e)
        {
            return e.GetType().Name;
        }
    }
}
