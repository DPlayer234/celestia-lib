using System.Text;
using CelestiaCS.Lib.Format;

namespace CelestiaTests.Lib;

public class StringExt
{
    [TestCase(new[] { "a", "b", "c", "d" }, "a,b,c;d")]
    [TestCase(new[] { "a", "b", "c" }, "a,b;c")]
    [TestCase(new[] { "a", "b" }, "a;b")]
    [TestCase(new[] { "a" }, "a")]
    [TestCase(new string[0], "")]
    public void NaturalJoin(string[] param, string result)
    {
        Assert.That(param.JoinNaturalText(";", ",").ToString(), Is.EqualTo(result));
        Assert.That($"{param.JoinNaturalText(";", ",")}", Is.EqualTo(result));
    }

    [TestCase(new[] { "a", "b", "c", "d" }, "a,b,c,d")]
    [TestCase(new[] { "a", "b", "c" }, "a,b,c")]
    [TestCase(new[] { "a", "b" }, "a,b")]
    [TestCase(new[] { "a" }, "a")]
    [TestCase(new string[0], "")]
    public void Join(string[] param, string result)
    {
        Assert.That(param.JoinText(",").ToString(), Is.EqualTo(result));
        Assert.That($"{param.JoinText(",")}", Is.EqualTo(result));
    }

    [TestCase("Hello **World!**", @"Hello \*\*World!\*\*")]
    [TestCase("@everyone", "@⁣everyone")] // Invis-char after @
    [TestCase("<@12345>", @"\<@⁣12345\>")] // Invis-char after @
    public void EscapeMarkdown_Success(string raw, string result)
    {
        Assert.That(raw, Is.Not.EqualTo(result));
        Assert.That(raw.EscapeMarkdown().ToString(), Is.EqualTo(result));
        Assert.That($"{raw.EscapeMarkdown()}", Is.EqualTo(result));
    }

    [TestCase("hello world!>")]
    [TestCase("hello@")]
    [TestCase("<world")]
    [TestCase("hello@world")]
    public void EscapeMarkdown_FailLimit(string data)
    {
        Span<char> target = stackalloc char[data.Length];
        Assert.That(data.EscapeMarkdown().TryFormat(target, out _), Is.False, "Not enough space.");
    }

    [TestCase(new[] { "Hello", "World", "Hey" }, 20, "Hello, World, Hey")]
    [TestCase(new[] { "Hello", "World", "Hey" }, 10, "...ld, Hey")]
    [TestCase(new[] { "Hello" }, 20, "Hello")]
    [TestCase(new[] { "Hello" }, 4, "...o")]
    [TestCase(new[] { "Hello", "World", "Hey" }, 2, "..")]
    [TestCase(new[] { "Hello" }, 5, "Hello")]
    public void JoinAndTruncateStart(string[] source, int maxLength, string result)
    {
        Assert.That(source.JoinAndTruncateStart(maxLength, joiner: ", ", truncation: "..."), Is.EqualTo(result));
    }

    [TestCase("Hello World!", 18, "...", "Hello World!")]
    [TestCase("Hello World!", 12, "...", "Hello World!")]
    [TestCase("Hello World!", 10, "...", "Hello W...")]
    public void Truncate(string source, int maxLength, string truncation, string result)
    {
        var truncated = source.Truncate(maxLength, truncation);

        Assert.That(truncated.ToString(), Is.EqualTo(result));
        Assert.That($"{truncated}", Is.EqualTo(result));
    }

    [TestCase("Hello World!", 18, "...", "Hello World!")]
    [TestCase("Hello World!", 12, "...", "Hello World!")]
    [TestCase("Hello World!", 10, "...", "Hello W...")]
    public void Truncate_Builder(string source, int maxLength, string truncation, string result)
    {
        var builder = new StringBuilder().Append(source);
        builder.Truncate(maxLength, truncation);

        Assert.That(builder.ToString(), Is.EqualTo(result));
    }
}
