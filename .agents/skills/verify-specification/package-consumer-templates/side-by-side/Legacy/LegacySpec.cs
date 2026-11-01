using Ardalis.Specification;

namespace Legacy;

public class LegacySpec : Specification<string>
{
    public LegacySpec() => Query.Where(s => s.StartsWith("A"));
}

public static class LegacyEvaluator
{
    public static IEnumerable<string> Evaluate(IEnumerable<string> source) => new LegacySpec().Evaluate(source);
    public static string Assembly => typeof(Specification<>).Assembly.FullName!;
}
