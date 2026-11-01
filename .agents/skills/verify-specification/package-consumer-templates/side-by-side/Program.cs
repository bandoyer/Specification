using OpenSpecification;

string[] names = ["Albania", "Norway", "Austria", "Belgium"];
Console.WriteLine($"legacy: {string.Join(", ", Legacy.LegacyEvaluator.Evaluate(names))}");
Console.WriteLine($"open: {string.Join(", ", new EndingWithY().Evaluate(names))}");
Console.WriteLine($"legacy assembly: {Legacy.LegacyEvaluator.Assembly}");
Console.WriteLine($"open assembly: {typeof(Specification<>).Assembly.FullName}");

public class EndingWithY : Specification<string>
{
    public EndingWithY() => Query.Where(s => s.EndsWith("y"));
}
