// The specifications the DbSet extension lines run. verify copies this file into both consumers, whose entities have the
// same shape, so each efcore-dbset-<name> line runs the specification its dbset-<name> line runs.
using OpenSpecification;

static class Reads
{
    public static Specification<Company> CompanyNamed(string name) => Spec<Company>(q => q.Where(c => c.Name == name));

    public static Specification<Company, string> CountryOf(string company) =>
        Spec<Company, string>(q => { q.Where(c => c.Name == company); q.Select(c => c.Country.Name); });

    public static SingleResultSpecification<Company> OnlyCompanyNamed(string name) => Single<Company>(q => q.Where(c => c.Name == name));

    public static SingleResultSpecification<Company> OnlyCompanyIn(string country) => Single<Company>(q => q.Where(c => c.Country.Name == country));

    public static SingleResultSpecification<Company, string> OnlyCountryOf(string company) =>
        Single<Company, string>(q => { q.Where(c => c.Name == company); q.Select(c => c.Country.Name); });

    // Four of the six products cost more than 6. The paging would leave two of them.
    public static Specification<Product> PricierThanSixPaged() => Spec<Product>(q => q.Where(p => p.Price > 6).OrderBy(p => p.Name).Skip(1).Take(2));

    public static Specification<Product> ProductNamed(string name, int skip = 0) => Spec<Product>(q => q.Where(p => p.Name == name).Skip(skip, skip > 0));

    public static Specification<Store, string> GlobexCities(bool reversed = false) => Spec<Store, string>(q =>
    {
        q.Where(s => s.Company.Name == "Globex").OrderBy(s => s.City);
        q.Select(s => s.City);
        if (reversed) q.PostProcessingAction(cities => cities.Reverse());
    });

    public static Specification<Store> StoresOf(string company) => Spec<Store>(q => q.Where(s => s.Company.Name == company));

    public static Specification<Store> StoresIn(string city) => Spec<Store>(q => q.Where(s => s.City == city));

    static Specification<T> Spec<T>(Action<ISpecificationBuilder<T>> build)
    {
        var spec = new Specification<T>();
        build(spec.Query);
        return spec;
    }

    static Specification<T, TResult> Spec<T, TResult>(Action<ISpecificationBuilder<T, TResult>> build)
    {
        var spec = new Specification<T, TResult>();
        build(spec.Query);
        return spec;
    }

    static SingleResultSpecification<T> Single<T>(Action<ISpecificationBuilder<T>> build)
    {
        var spec = new SingleResultSpecification<T>();
        build(spec.Query);
        return spec;
    }

    static SingleResultSpecification<T, TResult> Single<T, TResult>(Action<ISpecificationBuilder<T, TResult>> build)
    {
        var spec = new SingleResultSpecification<T, TResult>();
        build(spec.Query);
        return spec;
    }

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
