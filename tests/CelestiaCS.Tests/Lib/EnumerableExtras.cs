using CelestiaCS.Lib.State;

namespace CelestiaTests.Lib;

public class EnumerableExtras
{
    [Test]
    public void MaxByOrDefault()
    {
        int[] data = [7, 2, 9, 4, 12, 6];
        Assert.That(data.MaxByOrDefault(i => i), Is.EqualTo(12));
        Assert.That(data.MaxByOrDefault(i => i, Maybe<int>.None), Is.EqualTo(new Maybe<int>(12)));
        Assert.That(Array.Empty<int>().MaxByOrDefault(i => i, Maybe<int>.None), Is.EqualTo(Maybe<int>.None));
    }

    [Test]
    public void MinByOrDefault()
    {
        int[] data = [7, 2, 9, 4, 12, 6];
        Assert.That(data.MinByOrDefault(i => i), Is.EqualTo(2));
        Assert.That(data.MinByOrDefault(i => i, Maybe<int>.None), Is.EqualTo(new Maybe<int>(2)));
        Assert.That(Array.Empty<int>().MinByOrDefault(i => i, Maybe<int>.None), Is.EqualTo(Maybe<int>.None));
    }
}
