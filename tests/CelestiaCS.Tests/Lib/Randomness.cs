namespace CelestiaTests.Lib;

public class Randomness
{
    private const int RollCount = 9000;

    [Test]
    public void Integers()
    {
        const int Max = 50;
        var rng = new LocalRng();

        Dictionary<int, int> counts = [];
        int total = 0;
        for (int i = 0; i < RollCount; i++)
        {
            int picked = rng.Int(0, Max);

            counts.TryGetValue(picked, out int count);
            counts[picked] = count + 1;

            total += picked;
        }

        Console.WriteLine("Total count (x{1}): {0}", total, RollCount);
        foreach (var item in counts.OrderBy(c => c.Key)) Console.WriteLine("  {0} x{1}", item.Key, item.Value);

        Assert.That((double)total / RollCount, Is.EqualTo(Max * 0.5).Within(Max * 0.1), "Int Randomness seems off.");
    }

    [Test]
    public void Floats()
    {
        const double Max = 50d;
        var rng = new LocalRng();

        Dictionary<int, int> counts = [];
        double total = 0;
        for (int i = 0; i < RollCount; i++)
        {
            double picked = rng.Float(0d, Max);

            int intPicked = (int)picked;
            counts.TryGetValue(intPicked, out int count);
            counts[intPicked] = count + 1;

            total += picked;
        }

        Console.WriteLine("Total count (x{1}): {0}", total, RollCount);
        foreach (var item in counts.OrderBy(c => c.Key)) Console.WriteLine("  {0} x{1}", item.Key, item.Value);

        Assert.That(total / RollCount, Is.EqualTo(Max * 0.5).Within(Max * 0.1), "Float Randomness seems off.");
    }

    private sealed record RngVarianceMarker;
}
