using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CelestiaCS.Lib.Format.Items;

/// <summary>
/// A formattable item for some text with a prefixed count.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct CountFormatItem : IStringBuilderAppendable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CountFormatItem"/> struct.
    /// </summary>
    /// <param name="count"> The amount to show. </param>
    /// <param name="label"> The text to show. </param>
    /// <param name="flags"> Additional flags for formatting. </param>
    public CountFormatItem(int count, string label, CountFormatFlags flags = CountFormatFlags.None)
    {
        Count = count;
        Label = label;
        Flags = flags;
    }

    /// <summary> The amount to show. </summary>
    public int Count { get; }
    /// <summary> The text to show. </summary>
    public string Label { get; }
    /// <summary> Additional flags for formatting. </summary>
    public CountFormatFlags Flags { get; }

    /// <summary>
    /// Converts this instance into a new string.
    /// </summary>
    public override string ToString() => $"{this}";

    /// <summary>
    /// Tries to format this instance into a span.
    /// </summary>
    /// <param name="destination"> The destination span. </param>
    /// <param name="charsWritten"> The amount of characters written. </param>
    /// <returns> Whether the operation was successful. </returns>
    public bool TryFormat(Span<char> destination, out int charsWritten)
    {
        bool isBold = Flags.HasFlag(CountFormatFlags.IsBold);
        bool hasSpace = Flags.HasFlag(CountFormatFlags.HasSpace);
        var writer = new SpanFormatWriter(destination, out charsWritten);
        return writer.Append(Count)
            && (!hasSpace || writer.Append(' '))
            && (!isBold || writer.Append("**"))
            && writer.Append(Label)
            && (!isBold || writer.Append("**"));
    }

    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString();
    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => TryFormat(destination, out charsWritten);

    void IStringBuilderAppendable.AppendTo(StringBuilder builder, string? format)
    {
        bool isBold = Flags.HasFlag(CountFormatFlags.IsBold);
        bool hasSpace = Flags.HasFlag(CountFormatFlags.HasSpace);

        builder.Append(Count);
        if (hasSpace) builder.Append(' ');
        if (isBold) builder.Append("**");
        builder.Append(Label);
        if (isBold) builder.Append("**");
    }
}

/// <summary>
/// Additional flags for <see cref="CountFormatItem"/>.
/// </summary>
[Flags]
public enum CountFormatFlags : byte
{
    /// <summary> Nothing to consider. </summary>
    None = 0,
    /// <summary> Insert markdown for bold text around the label. </summary>
    IsBold = 0x1,
    /// <summary> Insert a space between the count and label. </summary>
    HasSpace = 0x2
}
