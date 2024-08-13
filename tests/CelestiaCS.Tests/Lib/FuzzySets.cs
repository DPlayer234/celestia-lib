using CelestiaCS.Lib.FuzzySets;

namespace CelestiaTests.Lib;

public class FuzzySets
{
    [Test]
    public void Core()
    {
        var fuzzySet = new FuzzySet<int>();
        fuzzySet.Add("i", 10_000_000);
        fuzzySet.Add("up", 20_000_000);
        fuzzySet.Add("who", 3);
        fuzzySet.Add("Hello World!", 42);
        fuzzySet.Add("Goodbye then.", 69);
        fuzzySet.Add("Good World!", 120);
        fuzzySet.Add("The Bee Movie Script", 420);
        fuzzySet.TrimLists();

        Assert.That(fuzzySet.Get("  hell  w o RLd").Select(p => p.Meta), Has.One.EqualTo(42));
        Assert.That(fuzzySet.Get("GOO--BYETH--EN").Select(p => p.Meta), Has.One.EqualTo(69));
        Assert.That(fuzzySet.Get("GOO WORLD").Select(p => p.Meta), Has.One.EqualTo(120));
        Assert.That(fuzzySet.Get("I").Select(p => p.Meta), Has.One.EqualTo(10_000_000));
        Assert.That(fuzzySet.Get("UP").Select(p => p.Meta), Has.One.EqualTo(20_000_000));
        Assert.That(fuzzySet.Get("WHO").Select(p => p.Meta), Has.One.EqualTo(3));
        Assert.That(fuzzySet.Get("E").Select(p => p.Meta), Is.Empty);
        Assert.That(fuzzySet.Get("By all knows laws of aviation [...]"), Is.Empty);

        // Also check multi-matches:
        Assert.That(fuzzySet.Get("GOOD", minMatchScore: 0).Select(p => p.Meta), Has.One.EqualTo(69) & Has.One.EqualTo(120));
    }

    [TestCase("Hello Wo")]
    [TestCase("rld!")]
    [TestCase("")]
    [TestCase("Oi?!")]
    [TestCase("-")]
    public void SegmentGenerate(string textUtf16)
    {
        var text = textUtf16;

        // This test works based on the logic that every correctly constructed segment is reversible.
        // Cases that don't have to work are not covered.
        Assert.That(text, Has.Length.LessThanOrEqualTo(FuzzySegment.MaxContentLength), "Invalid test case.");

        var segment = new FuzzySegment(text);
        Console.WriteLine("'{0}' = 0x{1:x}", text, segment.Value);
        
        Assert.That(segment.ToString(), Is.EqualTo(text));
        Assert.That(segment == new FuzzySegment(text));
        Assert.That(segment != new FuzzySegment(['+']));
    }

    [TestCase(new[] { "hello", "hey", "hi", "hey there" }, "he", new[] { "hello", "hey", "hey there" })]
    [TestCase(new[] { "hello", "he", "hi", "heya" }, "hell", new[] { "hello" })]
    [TestCase(new[] { "a", "b", "c", "d" }, "B", new[] { "b" })]
    [TestCase(new[] { "aaaaa", "aaaa", "aa" }, "b", new string[0])]
    [TestCase(new[] { "aaaaa", "aaaa", "aa" }, "ää", new[] { "aa", "aaaa", "aaaaa" })]
    public void PrefixSet(string[] set, string prefix, string[] matches)
    {
        var ups = UnicodePrefixSet.CreateFromInvariant(set);
        var results = ups.Find(prefix);
        Assert.That(results, Is.EquivalentTo(matches));
    }
}
