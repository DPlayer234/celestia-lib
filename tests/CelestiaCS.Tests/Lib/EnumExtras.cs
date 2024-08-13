namespace CelestiaTests.Lib;

public class EnumExtras
{
    [TestCase("default", TestEnum.Default)]
    [TestCase("a ", TestEnum.A)]
    [TestCase(" ex", TestEnum.Ex)]
    public void TryParseByName_Success(string name, TestEnum value)
    {
        Assert.That(EnumEx.TryParseByName(name, out TestEnum result), Is.True);
        Assert.That(result, Is.EqualTo(value));
    }

    [TestCase("Default", TestEnum.Default)]
    [TestCase("A", TestEnum.A)]
    [TestCase("Ex", TestEnum.Ex)]
    public void TryParseByExactName_Success(string name, TestEnum value)
    {
        Assert.That(EnumEx.TryParseByName(name, StringComparison.Ordinal, out TestEnum result), Is.True);
        Assert.That(result, Is.EqualTo(value));
    }

    [TestCase("hello")]
    [TestCase("0")]
    [TestCase("default,a")]
    public void TryParseByName_Failure(string name)
    {
        Assert.That(EnumEx.TryParseByName(name, out TestEnum result), Is.False);
        Assert.That(result, Is.EqualTo(default(TestEnum)));
    }

    [TestCase("ex")]
    [TestCase("hello")]
    [TestCase("0")]
    [TestCase("Default,A")]
    public void TryParseByExactName_Failure(string name)
    {
        Assert.That(EnumEx.TryParseByName(name, StringComparison.Ordinal, out TestEnum result), Is.False);
        Assert.That(result, Is.EqualTo(default(TestEnum)));
    }

    [TestCase((TestEnum)0)]
    [TestCase((TestFlagsEnum)0)]
    public void ToStringFast<T>(T _0) where T : struct, Enum
    {
        for (int i = 0; i < 128; i++)
        {
            T v = (T)(object)i;

            string f = v.ToStringFast();
            string c = v.ToString();

            Console.WriteLine("{2,3}: {0} == {1}", c, f, i);
            Assert.That(f, Is.EqualTo(c));
        }
    }

    [Test]
    public void SanityCheck()
    {
        Assert.That(Enum.TryParse("A,B,C", false, out TestEnum result1), Is.True);
        Assert.That(result1, Is.EqualTo(TestEnum.A | TestEnum.B | TestEnum.C));

        Assert.That(Enum.TryParse("0", false, out TestEnum result2), Is.True);
        Assert.That(result2, Is.EqualTo((TestEnum)0));

        Assert.That(Enum.TryParse("no", false, out TestEnum result3), Is.False);
        Assert.That(result3, Is.EqualTo(default(TestEnum)));
    }

    public enum TestEnum
    {
        Default = default,
        A, B, C,
        Ex = 127
    }

    [Flags]
    public enum TestFlagsEnum
    {
        None = 0,
        A = 1 << 0,
        B = 1 << 1,
        C = 1 << 2,
        D = 1 << 3,
        All = A | B | C | D
    }
}
