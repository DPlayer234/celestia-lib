namespace CelestiaTests.Lib;

public class Timers
{
    public static IEnumerable<long> TestCases =>
    [
        1,
        5,
        8,
        10,
        1000,
        1000000
    ];

    [TestCaseSource(nameof(TestCases))]
    public void IntTimeSpan_FromMilliseconds(long c)
    {
        Assert.That(TimeSpan.FromMilliseconds(c), Is.EqualTo(TimeSpan.FromMilliseconds(c)));
    }

    [TestCaseSource(nameof(TestCases))]
    public void IntTimeSpan_FromSeconds(long c)
    {
        Assert.That(TimeSpan.FromSeconds(c), Is.EqualTo(TimeSpan.FromSeconds(c)));
    }

    [TestCaseSource(nameof(TestCases))]
    public void IntTimeSpan_FromMinutes(long c)
    {
        Assert.That(TimeSpan.FromMinutes(c), Is.EqualTo(TimeSpan.FromMinutes(c)));
    }
}
