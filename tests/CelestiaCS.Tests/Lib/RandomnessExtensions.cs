using CelestiaCS.Lib.Memory;
using NUnit.Framework.Constraints;

namespace CelestiaTests.Lib;

public class RandomnessExtensions
{
    private const int RollCount = 9000;

    private readonly (int, double)[] _weights =
    [
        (1, 1.0),
        (2, 2.0),
        (3, 3.0),
        (4, 4.0),
        (5, 5.0),
        (6, 6.0),
        (7, 7.0),
        (8, 8.0),
        (9, 9.0)
    ];

    private static IResolveConstraint TotalWeightConstraint => Is.EqualTo(6.33).Within(0.5);

    private readonly IReadOnlyList<int> _sourceData = Enumerable.Range(0, 100).ToReadOnlyList();

    [TestCase(0.00, 0)]
    [TestCase(0.01, 1)]
    [TestCase(0.09, 9)]
    [TestCase(0.41, 41)]
    [TestCase(0.99, 99)]
    public void ListItem(double rngFactor, int result)
    {
        var rng = new TestRng().Fixed(rngFactor);
        int item = rng.Item(_sourceData);
        Assert.That(item, Is.EqualTo(result));
    }

    [TestCase(0.00, 0)]
    [TestCase(0.04, 0)]
    [TestCase(0.09, 5)]
    [TestCase(0.22, 20)]
    [TestCase(0.41, 40)]
    [TestCase(0.99, 95)]
    public void ListItem_WhereMod5Eq0(double rngFactor, int result)
    {
        var rng = new TestRng().Fixed(rngFactor);
        int item = rng.Item(_sourceData.Where(i => i % 5 == 0));
        Assert.That(item, Is.EqualTo(result));
    }

    [Test]
    public void WeightedOnce_SpanInit()
    {
        var rng = new LocalRng();

        Dictionary<int, int> counts = [];
        double total = 0;
        for (int i = 0; i < RollCount; i++)
        {
            int picked = rng.PickWeightedItem(_weights.AsReadOnlySpan());

            counts.TryGetValue(picked, out int count);
            counts[picked] = count + 1;

            total += picked;
        }

        Console.WriteLine("Total count (x{1}): {0}", total, RollCount);
        foreach (var item in counts.OrderBy(c => c.Key)) Console.WriteLine("  {0} x{1}", item.Key, item.Value);

        Assert.That(total / RollCount, TotalWeightConstraint, "Single Weighted Randomness seems off.");
    }

    [Test]
    public void WeightedPicker_SpanInit()
    {
        var rng = new LocalRng();
        var picker = rng.CreateWeightedPicker(_weights.AsReadOnlySpan());

        Dictionary<int, int> counts = [];
        double total = 0;
        for (int i = 0; i < RollCount; i++)
        {
            int picked = picker.Next();

            counts.TryGetValue(picked, out int count);
            counts[picked] = count + 1;

            total += picked;
        }

        Console.WriteLine("Total count (x{1}): {0}", total, RollCount);
        foreach (var item in counts.OrderBy(c => c.Key)) Console.WriteLine("  {0} x{1}", item.Key, item.Value);

        Assert.That(total / RollCount, TotalWeightConstraint, "Picker Randomness seems off.");
    }

    [Test]
    public void WeightedOnce_EnumerableInit()
    {
        var rng = new LocalRng();
        var pairs = _weights.Select(x => x);

        Dictionary<int, int> counts = [];
        double total = 0;
        for (int i = 0; i < RollCount; i++)
        {
            int picked = rng.PickWeightedItem(pairs);

            counts.TryGetValue(picked, out int count);
            counts[picked] = count + 1;

            total += picked;
        }

        Console.WriteLine("Total count (x{1}): {0}", total, RollCount);
        foreach (var item in counts.OrderBy(c => c.Key)) Console.WriteLine("  {0} x{1}", item.Key, item.Value);

        Assert.That(total / RollCount, TotalWeightConstraint, "Single Weighted Randomness seems off.");
    }

    [Test]
    public void WeightedPicker_EnumerableInit()
    {
        var rng = new LocalRng();
        var picker = rng.CreateWeightedPicker(_weights.Select(x => x));

        Dictionary<int, int> counts = [];
        double total = 0;
        for (int i = 0; i < RollCount; i++)
        {
            int picked = picker.Next();

            counts.TryGetValue(picked, out int count);
            counts[picked] = count + 1;

            total += picked;
        }

        Console.WriteLine("Total count (x{1}): {0}", total, RollCount);
        foreach (var item in counts.OrderBy(c => c.Key)) Console.WriteLine("  {0} x{1}", item.Key, item.Value);

        Assert.That(total / RollCount, TotalWeightConstraint, "Picker Randomness seems off.");
    }

    [Test]
    public void WeightedPicker_ExceptionThrows()
    {
        Assert.Multiple(() =>
        {
            var rng = new LocalRng();

            // Span/Array based overloads
            Assert.Throws<ArgumentException>(() => rng.PickWeightedItem(ReadOnlySpan<(int, double)>.Empty));
            Assert.Throws<ArgumentException>(() => rng.PickWeightedItem([(1, 0.0)]));
            Assert.Throws<ArgumentException>(() => rng.PickWeightedItem([(1, -1.0)]));
            Assert.Throws(Is.Null, () => rng.PickWeightedItem([(1, 0.0), (2, 0.1)]));

            Assert.Throws<ArgumentException>(() => rng.CreateWeightedPicker(ReadOnlySpan<(int, double)>.Empty));
            Assert.Throws<ArgumentException>(() => rng.CreateWeightedPicker([(1, 0.0)]));
            Assert.Throws<ArgumentException>(() => rng.CreateWeightedPicker([(1, -1.0)]));
            Assert.Throws(Is.Null, () => rng.CreateWeightedPicker([(1, 0.0), (2, 0.1)]).Next());

            // Enumerable based overloads
            Assert.Throws<ArgumentException>(() => rng.PickWeightedItem(Enumerable.Empty<(int, double)>()));
            Assert.Throws<ArgumentException>(() => rng.PickWeightedItem(new[] { (1, 0.0) }.ToList()));
            Assert.Throws<ArgumentException>(() => rng.PickWeightedItem(new[] { (1, -1.0) }.ToList()));
            Assert.Throws(Is.Null, () => rng.PickWeightedItem(new[] { (1, 0.0), (2, 0.1) }.ToList()));

            Assert.Throws<ArgumentException>(() => rng.CreateWeightedPicker(Enumerable.Empty<(int, double)>()));
            Assert.Throws<ArgumentException>(() => rng.CreateWeightedPicker(new[] { (1, 0.0) }.ToList()));
            Assert.Throws<ArgumentException>(() => rng.CreateWeightedPicker(new[] { (1, -1.0) }.ToList()));
            Assert.Throws(Is.Null, () => rng.CreateWeightedPicker(new[] { (1, 0.0), (2, 0.1) }.ToList()).Next());
        });
    }

    [Test, Repeat(3)]
    public void UniquePicker_Uniqueness()
    {
        Assert.Multiple(() =>
        {
            var rng = new LocalRng();

            int[] source = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            var picker = rng.CreateUniquePicker(source);

            List<int> result = [];
            while (picker.AnyLeft())
            {
                result.Add(picker.Next());
            }

            Assert.That(result, Is.EquivalentTo(source));
        });
    }

    [Test]
    public void UniquePicker_ExceptionThrows()
    {
        Assert.Multiple(() =>
        {
            var rng = new LocalRng();

            Assert.Throws<ArgumentException>(() => rng.PickUniqueItems(Array.Empty<int>(), 1));
            Assert.Throws<InvalidOperationException>(() => rng.CreateUniquePicker(Array.Empty<int>()).Next());
        });
    }
}
