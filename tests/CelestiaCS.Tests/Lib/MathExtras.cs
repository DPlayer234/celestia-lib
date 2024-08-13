namespace CelestiaTests.Lib;

public class MathExtras
{
    [TestCase(3, 4, ExpectedResult = 3)]
    [TestCase(-3, 4, ExpectedResult = 1)]
    [TestCase(3, -4, ExpectedResult = -1)]
    [TestCase(-3, -4, ExpectedResult = -3)]
    [TestCase(7, 4, ExpectedResult = 3)]
    [TestCase(-7, 4, ExpectedResult = 1)]
    [TestCase(7, -4, ExpectedResult = -1)]
    [TestCase(-7, -4, ExpectedResult = -3)]
    [TestCase(3, 3, ExpectedResult = 0)]
    [TestCase(-3, 3, ExpectedResult = 0)]
    [TestCase(3, -3, ExpectedResult = 0)]
    [TestCase(-3, -3, ExpectedResult = 0)]
    [TestCase(0, 15, ExpectedResult = 0)]
    [TestCase(0, -15, ExpectedResult = 0)]
    public int Remainder(int a, int b)
    {
        return MathEx.Mod(a, b);
    }

    [TestCase(0, 0, ExpectedResult = 0)]
    [TestCase(200, 1502, ExpectedResult = 200 + 1502)]
    [TestCase(20, -1502, ExpectedResult = 20 - 1502)]
    [TestCase(long.MaxValue, 100, ExpectedResult = long.MaxValue)]
    [TestCase(long.MinValue, -100, ExpectedResult = long.MinValue)]
    public long LimitAdd(long a, long b)
    {
        return MathEx.LimitAdd(a, b);
    }

    [TestCase(1, 0, 2, 1, 9, ExpectedResult = 5)]
    [TestCase(0, 0, 15, 24, 48, ExpectedResult = 24)]
    [TestCase(15, 0, 15, 24, 48, ExpectedResult = 48)]
    [TestCase(15, 0, 15, 5, 2, ExpectedResult = 2)]
    [TestCase(15, 15, 0, 5, 2, ExpectedResult = 5)]
    public int Map(int value, int fromMin, int fromMax, int toMin, int toMax)
    {
        return MathEx.Map(value, fromMin, fromMax, toMin, toMax);
    }
}
