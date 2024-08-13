using System.Text;
using CelestiaCS.Lib.Format;

namespace CelestiaTests.Lib;

public class FormatWriter
{
    private const int ExpectedLength = 6 + 4 + (3 + 4) + 8;
    private static readonly string _expectedText = $"6 long{5432}{10.24:F4}... yeah";

    [Test]
    public void Writer_EnoughSpace()
    {
        Span<char> buffer = stackalloc char[ExpectedLength];
        var writer = new SpanFormatWriter(buffer, out int written);

        Assert.That(TestWrite(ref writer), Is.True);
        Assert.That(written, Is.EqualTo(ExpectedLength));

        Console.WriteLine(buffer[..written].ToString());
    }

    [Test]
    public void Writer_NotEnoughSpace()
    {
        Span<char> buffer = stackalloc char[ExpectedLength / 2];
        var writer = new SpanFormatWriter(buffer, out int written);

        Assert.That(TestWrite(ref writer), Is.False);
        Assert.That(written, Is.GreaterThan(0) & Is.LessThan(ExpectedLength));

        Console.WriteLine(buffer[..written].ToString());
    }

    [Test]
    public void Repeat_Char()
    {
        string repeat = $"={FormatUtil.Repeat('c', 8)}=";
        Assert.That(repeat, Is.EqualTo("=cccccccc="));
    }

    [Test]
    public void Repeat_String()
    {
        string repeat = $"={FormatUtil.Repeat("ab", 5)}=";
        Assert.That(repeat, Is.EqualTo("=ababababab="));
    }

    [Test]
    public void Repeat_Other()
    {
        string repeat = $"={FormatUtil.Repeat(231, 5)}=";
        Assert.That(repeat, Is.EqualTo("=231231231231231="));
    }

    [TestCase(0.50, "[====----]")]
    [TestCase(0.25, "[==------]")]
    [TestCase(0.45, "[===-----]")]
    public void Bar(double progress, string result)
    {
        string repeat = $"[{FormatUtil.BarRel('=', '-', width: 8, progress)}]";
        Assert.That(repeat, Is.EqualTo(result));
    }

    [Test]
    public void Temp_ToString()
    {
        var temp = new TempString($"6 long{5432}{10.24:F4}... yeah");
        var perm = temp.ToString();

        Assert.That(perm, Is.EqualTo(_expectedText));
    }

    [Test]
    public void Temp_TryFormat_EnoughSpace()
    {
        Span<char> buffer = stackalloc char[ExpectedLength];
        var temp = new TempString($"6 long{5432}{10.24:F4}... yeah");

        Assert.That(temp.TryFormat(buffer, out int written), Is.True);
        Assert.That(written, Is.EqualTo(ExpectedLength));

        Console.WriteLine(buffer[..written].ToString());
        Assert.That(buffer[..written].ToString(), Is.EqualTo(_expectedText));
    }

    [Test]
    public void Temp_TryFormat_NotEnoughSpace()
    {
        Span<char> buffer = stackalloc char[ExpectedLength / 2];
        var temp = new TempString($"6 long{5432}{10.24:F4}... yeah");

        Assert.That(temp.TryFormat(buffer, out int written), Is.False);
        Assert.That(written, Is.LessThan(ExpectedLength));

        Console.WriteLine(buffer[..written].ToString());
    }

    [Test]
    public void Temp_ToString_MultiThrows()
    {
        AssertEx.DebugOnly();

        Assert.Throws<InvalidOperationException>(() =>
        {
            var temp = new TempString($"6 long{5432}{10.24:F4}... yeah");
            _ = temp.ToString();
            _ = temp.ToString();
        });
    }

    [Test]
    public void Temp_TryFormat_MultiSuccessThrows()
    {
        AssertEx.DebugOnly();

        Assert.Throws<InvalidOperationException>(() =>
        {
            Span<char> buffer = stackalloc char[ExpectedLength];

            var temp = new TempString($"6 long{5432}{10.24:F4}... yeah");

            bool ok1 = temp.TryFormat(buffer, out int w1);
            Assert.That(ok1, Is.True);

            bool ok2 = temp.TryFormat(buffer, out int w2);
        });
    }

    [Test]
    public void Temp_TryFormat_UseAfterFailureOkay()
    {
        Assert.Throws(Is.Null, () =>
        {
            Span<char> buffer1 = stackalloc char[ExpectedLength / 2];

            var temp = new TempString($"6 long{5432}{10.24:F4}... yeah");

            bool ok1 = temp.TryFormat(buffer1, out _);
            Assert.That(ok1, Is.False);

            Span<char> buffer2 = stackalloc char[ExpectedLength];

            bool ok2 = temp.TryFormat(buffer2, out _);
            Assert.That(ok2, Is.True);
        });
    }

    [Test]
    public void Temp_Truncate()
    {
        TempString result = new TempString($"Hello World!").Truncate(4);
        Assert.That(result.ToString(), Is.EqualTo("Hell"));
    }

    [Test]
    public void Temp_Truncate_WithTruncation()
    {
        TempString result = new TempString($"Hello World!").Truncate(6, "p!!");
        Assert.That(result.ToString(), Is.EqualTo("Help!!"));
    }

    [Test]
    public void Temp_Truncate_DoesNotTruncate()
    {
        TempString result = new TempString($"Hello World!").Truncate(24);
        Assert.That(result.ToString(), Is.EqualTo("Hello World!"));
    }

    [Test]
    public void Temp_Truncate_DoesNotTruncateWithTruncation()
    {
        TempString result = new TempString($"Hello World!").Truncate(24, "p!!");
        Assert.That(result.ToString(), Is.EqualTo("Hello World!"));
    }

    [Test]
    public void SB()
    {
        string result = FormatUtil.SB($"6 long{5432}{10.24:F4}... yeah");
        Assert.That(result, Is.EqualTo(_expectedText));
    }

    [Test]
    public void SB_AppendEx()
    {
        StringBuilder builder = new();
        builder.AppendEx($"6 long{5432}{10.24:F4}... yeah");

        Assert.That(builder.ToString(), Is.EqualTo(_expectedText));
    }

    [Test]
    public void FormatString()
    {
        Assert.That(new FormatString("Hello, {0}! How are {1}?").Format("World", "you"), Is.EqualTo("Hello, World! How are you?"));
        Assert.That(new FormatString("{0}{1}{0}").Format(42, "{5}"), Is.EqualTo("42{5}42"));
        Assert.That(new FormatString("h {{0}} h").Format("never"), Is.EqualTo("h {0} h"));
        Assert.That(new FormatString("h {{0:D2}} h").Format("never"), Is.EqualTo("h {0:D2} h"));
        Assert.That(new FormatString("A: {0:F2}, B: {1:X}").Format(1.2, 15), Is.EqualTo("A: 1.20, B: F"));
        Assert.That(new FormatString("").Text, Is.EqualTo(""));

        Assert.Throws<ArgumentException>(() => new FormatString("{0}}"));
        Assert.Throws<ArgumentException>(() => new FormatString("{{0}"));
        Assert.Throws<ArgumentException>(() => new FormatString("}{0}"));
        Assert.Throws<ArgumentException>(() => new FormatString("{0}{"));
        Assert.Throws<ArgumentException>(() => new FormatString("{a}"));
        Assert.Throws<ArgumentException>(() => new FormatString("{a:}}"));
    }

    private bool TestWrite(ref SpanFormatWriter writer)
    {
        return writer.Append("6 long")
            && writer.Append(5432)
            && writer.Append(10.24, "F4")
            && writer.Append("... yeah");
    }
}
