using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using CelestiaCS.Lib.Collections;
using CelestiaCS.Lib.Json;

namespace CelestiaCS.Lib.Format;

/// <summary>
/// Allows interpolating strings mostly allocation-free with format strings defined at runtime.
/// </summary>
/// <remarks>
/// Instantiating this struct pre-parses the provided format string and is intended mostly for
/// repeated use/to front-load the costs associated with using format strings.
/// Alignment of format holes is not supported.
/// </remarks>
[JsonConverter(typeof(JsonFormatStringConverter))]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly struct FormatString : IEquatable<FormatString>
{
    // CompositeFormat in the BCL now does basically the same thing,
    // but I prefer this API and it's in use anyways now so...

    internal readonly string text;
    internal readonly FormatHole[] holes;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormatString"/> struct.
    /// </summary>
    /// <remarks>
    /// This pre-parses the format string so interpolations are faster.
    /// </remarks>
    /// <param name="fmt"> The format to use. </param>
    /// <exception cref="ArgumentException"> The format string is invalid. </exception>
    public FormatString(ReadOnlySpan<char> fmt)
    {
        Marker marker = default;
        ValueStringBuilder builder = new(stackalloc char[256]);
        Writer.Parse(ref builder, fmt, ref marker);

        text = builder.ToStringAndDispose();
        holes = marker.Holes.DrainToArray();
    }

    private FormatString(string text, FormatHole[] holes)
    {
        this.text = text;
        this.holes = holes;
    }

    /// <summary> Escapes a string to be able to be used as a format string with no holes. </summary>
    /// <param name="raw"> The string to escape. </param>
    /// <returns> The escaped format string. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="raw"/> is null. </exception>
    public static string Escape(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);

        // Don't bother optimizing this, it's rarely used.
        return raw
            .Replace("{", "{{", StringComparison.Ordinal)
            .Replace("}", "}}", StringComparison.Ordinal);
    }

    /// <summary> Creates a format string with no holes that just returns the provided text. </summary>
    /// <param name="text"> The text. </param>
    /// <returns> A format string representing the raw text. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="text"/> is null. </exception>
    public static FormatString Raw(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new FormatString(text, holes: []);
    }

    /// <summary> Gets the raw text with the format holes removed, if there were any. </summary>
    public string Text
    {
        get
        {
            var text = this.text;
            ThrowHelper.NullRefIfNull(text);
            return text;
        }
    }

    /// <summary> Gets information about the format holes. </summary>
    public ImmutableArray<FormatHole> Holes
    {
        get
        {
            var holes = this.holes;
            ThrowHelper.NullRefIfNull(holes);
            return ImmutableCollectionsMarshal.AsImmutableArray(holes);
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"f\"{ToString()}\"";

    // Basically uses reference equality on the 'holes' array.
    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is FormatString fmt && Equals(fmt);
    /// <inheritdoc/>
    public bool Equals(FormatString other) => holes == other.holes;
    /// <inheritdoc/>
    public override int GetHashCode() => holes.GetHashCode();

    #region Format overloads

    /// <include file='FormatString.Docs.xml' path='FormatString/Format'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/VarArgs'/>
    public string Format(params object?[] args)
    {
        FormatItemsVarArgs<object?> p = new(args);
        return Writer.Format(this, ref p);
    }

    /// <include file='FormatString.Docs.xml' path='FormatString/Format'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/SingleArg'/>
    public string Format<T>(T arg)
    {
        FormatItems<T> p;
        p.Item0 = arg;
        return Writer.Format(this, ref p);
    }

    /// <include file='FormatString.Docs.xml' path='FormatString/Format'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/MultiArgs'/>
    public string Format<T0, T1>(T0 arg0, T1 arg1)
    {
        FormatItems<T0, T1> p;
        p.Item0 = arg0;
        p.Item1 = arg1;
        return Writer.Format(this, ref p);
    }

    /// <include file='FormatString.Docs.xml' path='FormatString/Format'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/MultiArgs'/>
    public string Format<T0, T1, T2>(T0 arg0, T1 arg1, T2 arg2)
    {
        FormatItems<T0, T1, T2> p;
        p.Item0 = arg0;
        p.Item1 = arg1;
        p.Item2 = arg2;
        return Writer.Format(this, ref p);
    }

    /// <include file='FormatString.Docs.xml' path='FormatString/Format'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/MultiArgs'/>
    public string Format<T0, T1, T2, T3>(T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        FormatItems<T0, T1, T2, T3> p;
        p.Item0 = arg0;
        p.Item1 = arg1;
        p.Item2 = arg2;
        p.Item3 = arg3;
        return Writer.Format(this, ref p);
    }

    #endregion

    #region FormatTemp overloads

    /// <include file='FormatString.Docs.xml' path='FormatString/FormatTemp'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/VarArgs'/>
    public TempString FormatTemp(params object?[] args)
    {
        FormatItemsVarArgs<object?> p = new(args);
        return Writer.FormatTemp(this, ref p);
    }

    /// <include file='FormatString.Docs.xml' path='FormatString/FormatTemp'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/SingleArg'/>
    public TempString FormatTemp<T>(T arg)
    {
        FormatItems<T> p;
        p.Item0 = arg;
        return Writer.FormatTemp(this, ref p);
    }

    /// <include file='FormatString.Docs.xml' path='FormatString/FormatTemp'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/MultiArgs'/>
    public TempString FormatTemp<T0, T1>(T0 arg0, T1 arg1)
    {
        FormatItems<T0, T1> p;
        p.Item0 = arg0;
        p.Item1 = arg1;
        return Writer.FormatTemp(this, ref p);
    }

    /// <include file='FormatString.Docs.xml' path='FormatString/FormatTemp'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/MultiArgs'/>
    public TempString FormatTemp<T0, T1, T2>(T0 arg0, T1 arg1, T2 arg2)
    {
        FormatItems<T0, T1, T2> p;
        p.Item0 = arg0;
        p.Item1 = arg1;
        p.Item2 = arg2;
        return Writer.FormatTemp(this, ref p);
    }

    /// <include file='FormatString.Docs.xml' path='FormatString/FormatTemp'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/MultiArgs'/>
    public TempString FormatTemp<T0, T1, T2, T3>(T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        FormatItems<T0, T1, T2, T3> p;
        p.Item0 = arg0;
        p.Item1 = arg1;
        p.Item2 = arg2;
        p.Item3 = arg3;
        return Writer.FormatTemp(this, ref p);
    }

    #endregion

    #region TryFormat overloads

    /// <include file='FormatString.Docs.xml' path='FormatString/TryFormat'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/VarArgs'/>
    public bool TryFormat(Span<char> destination, out int charsWritten, params object?[] args)
    {
        FormatItemsVarArgs<object?> p = new(args);
        return Writer.TryFormat(this, destination, ref p, out charsWritten);
    }

    /// <include file='FormatString.Docs.xml' path='FormatString/TryFormat'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/SingleArg'/>
    public bool TryFormat<T>(Span<char> destination, out int charsWritten, T arg)
    {
        FormatItems<T> p;
        p.Item0 = arg;
        return Writer.TryFormat(this, destination, ref p, out charsWritten);
    }

    /// <include file='FormatString.Docs.xml' path='FormatString/TryFormat'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/MultiArgs'/>
    public bool TryFormat<T0, T1>(Span<char> destination, out int charsWritten, T0 arg0, T1 arg1)
    {
        FormatItems<T0, T1> p;
        p.Item0 = arg0;
        p.Item1 = arg1;
        return Writer.TryFormat(this, destination, ref p, out charsWritten);
    }

    /// <include file='FormatString.Docs.xml' path='FormatString/TryFormat'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/MultiArgs'/>
    public bool TryFormat<T0, T1, T2>(Span<char> destination, out int charsWritten, T0 arg0, T1 arg1, T2 arg2)
    {
        FormatItems<T0, T1, T2> p;
        p.Item0 = arg0;
        p.Item1 = arg1;
        p.Item2 = arg2;
        return Writer.TryFormat(this, destination, ref p, out charsWritten);
    }

    /// <include file='FormatString.Docs.xml' path='FormatString/TryFormat'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/MultiArgs'/>
    public bool TryFormat<T0, T1, T2, T3>(Span<char> destination, out int charsWritten, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        FormatItems<T0, T1, T2, T3> p;
        p.Item0 = arg0;
        p.Item1 = arg1;
        p.Item2 = arg2;
        p.Item3 = arg3;
        return Writer.TryFormat(this, destination, ref p, out charsWritten);
    }

    #endregion

    /// <summary> Recreates and returns the original format string. </summary>
    /// <returns> The original format string. </returns>
    public override string ToString()
    {
        FormatItemsVarArgs<FormatHole> p = new(holes);
        return Writer.Format(this, ref p);
    }

    public static bool operator ==(FormatString left, FormatString right) => left.Equals(right);
    public static bool operator !=(FormatString left, FormatString right) => !(left == right);
}

/// <summary> Represents a format hole. </summary>
/// <param name="Index"> The index in the text where this hole's value is inserted. </param>
/// <param name="Slot"> The input slot to insert here. </param>
/// <param name="Format"> The format string to provide to the insertion. </param>
public readonly record struct FormatHole(int Index, int Slot, string? Format) : ISpanFormattable
{
    /// <summary> Gets the text the format hole represents. </summary>
    /// <returns> The hole text. </returns>
    public override string ToString()
    {
        ValueStringBuilder result = new(stackalloc char[10 + 3 + 32]);
        result.Append(this);
        return result.ToStringAndDispose();
    }

    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString();

    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        var writer = new SpanFormatWriter(destination, out charsWritten);
        return writer.Append('{')
            && writer.Append(Slot)
            && (Format is null || (writer.Append(':') && writer.Append(Format)))
            && writer.Append('}');
    }
}

/// <summary>
/// Provides extension methods to use <see cref="FormatString"/> with string builders.
/// </summary>
public static class FormatStringExtensions
{
    #region AppendFormat to StringBuilder

    /// <include file='FormatString.Docs.xml' path='FormatString/AppendFormat'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/VarArgs'/>
    public static StringBuilder AppendFormat(this StringBuilder builder, FormatString fmt, params object?[] args)
    {
        FormatItemsVarArgs<object?> p = new(args);
        return Writer.WriteTo(builder, fmt, ref p);
    }

    /// <include file='FormatString.Docs.xml' path='FormatString/AppendFormat'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/SingleArg'/>
    public static StringBuilder AppendFormat<T>(this StringBuilder builder, FormatString fmt, T arg)
    {
        FormatItems<T> p;
        p.Item0 = arg;
        return Writer.WriteTo(builder, fmt, ref p);
    }

    /// <include file='FormatString.Docs.xml' path='FormatString/AppendFormat'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/MultiArgs'/>
    public static StringBuilder AppendFormat<T0, T1>(this StringBuilder builder, FormatString fmt, T0 arg0, T1 arg1)
    {
        FormatItems<T0, T1> p;
        p.Item0 = arg0;
        p.Item1 = arg1;
        return Writer.WriteTo(builder, fmt, ref p);
    }

    /// <include file='FormatString.Docs.xml' path='FormatString/AppendFormat'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/MultiArgs'/>
    public static StringBuilder AppendFormat<T0, T1, T2>(this StringBuilder builder, FormatString fmt, T0 arg0, T1 arg1, T2 arg2)
    {
        FormatItems<T0, T1, T2> p;
        p.Item0 = arg0;
        p.Item1 = arg1;
        p.Item2 = arg2;
        return Writer.WriteTo(builder, fmt, ref p);
    }

    /// <include file='FormatString.Docs.xml' path='FormatString/AppendFormat'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/MultiArgs'/>
    public static StringBuilder AppendFormat<T0, T1, T2, T3>(this StringBuilder builder, FormatString fmt, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        FormatItems<T0, T1, T2, T3> p;
        p.Item0 = arg0;
        p.Item1 = arg1;
        p.Item2 = arg2;
        p.Item3 = arg3;
        return Writer.WriteTo(builder, fmt, ref p);
    }

    #endregion

    #region AppendFormat to ValueStringBuilder

    /// <include file='FormatString.Docs.xml' path='FormatString/AppendFormat'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/VarArgs'/>
    public static void AppendFormat(ref this ValueStringBuilder builder, FormatString fmt, params object?[] args)
    {
        FormatItemsVarArgs<object?> p = new(args);
        Writer.WriteTo(ref builder, fmt, ref p);
    }

    /// <include file='FormatString.Docs.xml' path='FormatString/AppendFormat'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/SingleArg'/>
    public static void AppendFormat<T>(ref this ValueStringBuilder builder, FormatString fmt, T arg)
    {
        FormatItems<T> p;
        p.Item0 = arg;
        Writer.WriteTo(ref builder, fmt, ref p);
    }

    /// <include file='FormatString.Docs.xml' path='FormatString/AppendFormat'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/MultiArgs'/>
    public static void AppendFormat<T0, T1>(ref this ValueStringBuilder builder, FormatString fmt, T0 arg0, T1 arg1)
    {
        FormatItems<T0, T1> p;
        p.Item0 = arg0;
        p.Item1 = arg1;
        Writer.WriteTo(ref builder, fmt, ref p);
    }

    /// <include file='FormatString.Docs.xml' path='FormatString/AppendFormat'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/MultiArgs'/>
    public static void AppendFormat<T0, T1, T2>(ref this ValueStringBuilder builder, FormatString fmt, T0 arg0, T1 arg1, T2 arg2)
    {
        FormatItems<T0, T1, T2> p;
        p.Item0 = arg0;
        p.Item1 = arg1;
        p.Item2 = arg2;
        Writer.WriteTo(ref builder, fmt, ref p);
    }

    /// <include file='FormatString.Docs.xml' path='FormatString/AppendFormat'/>
    /// <include file='FormatString.Docs.xml' path='FormatString/MultiArgs'/>
    public static void AppendFormat<T0, T1, T2, T3>(ref this ValueStringBuilder builder, FormatString fmt, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        FormatItems<T0, T1, T2, T3> p;
        p.Item0 = arg0;
        p.Item1 = arg1;
        p.Item2 = arg2;
        p.Item3 = arg3;
        Writer.WriteTo(ref builder, fmt, ref p);
    }

    #endregion
}

file static class Writer
{
    /// <summary> Parses a format string. Can be used to write a formatted text or other things. </summary>
    /// <typeparam name="T"> The "part" handler's type. </typeparam>
    /// <param name="builder"> The builder to append text to. </param>
    /// <param name="fmt"> The format string. </param>
    /// <param name="part"> The "part" handler. </param>
    internal static void Parse<T>(ref ValueStringBuilder builder, ReadOnlySpan<char> fmt, ref T part) where T : struct, IFormatItem
    {
        int index;
        int read = 0;
        while (read < fmt.Length && (index = fmt[read..].IndexOfAny('{', '}')) >= 0)
        {
            index += read;

            // If what we found is the last character, we either have an
            // incomplete hole or an unescaped closing brace. Both are invalid.
            if (index == fmt.Length - 1)
                goto InvalidFormat;

            char brace = fmt[index];

            int endPrev = index;
            index += 1;

            char ch = fmt[index];
            if (brace == '{' && ch != '{')
            {
                // If we have an opening brace, and the next isn't an escape,
                // it's a format hole, insert slot for that
                int slot = 0;
                while (ch is >= '0' and <= '9')
                {
                    slot = slot * 10 + (ch - '0');

                    index += 1;
                    ch = fmt[index];
                }

                string? format = null;
                if (ch == ':')
                {
                    index += 1;
                    int len = fmt[index..].IndexOf('}');
                    if (len < 0) goto InvalidFormat;

                    format = fmt.Slice(index, len).ToString();
                    index += len;
                }
                else if (ch != '}')
                {
                    goto InvalidFormat;
                }

                builder.Append(fmt[read..endPrev]);
                part.AppendTo(ref builder, slot, format);

                read = index + 1;
            }
            else if (brace == '}' && ch != '}')
            {
                // If we hit an unescaped closing brace, the format string is invalid.
                goto InvalidFormat;
            }
            else
            {
                builder.Append(fmt[read..index]);

                read = index + 1;
            }
        }

        builder.Append(fmt[read..]);
        return;

    InvalidFormat:
        ThrowHelper.Argument(nameof(fmt), "Format string contained invalid format.");
    }

    /// <summary> Formats parts to a string via a <see cref="FormatString"/>. </summary>
    internal static string Format<T>(FormatString dynFmt, ref T part) where T : struct, IFormatItem
    {
        if (dynFmt.holes.Length == 0) return dynFmt.text;

        ValueStringBuilder builder = new(stackalloc char[256]);
        WriteTo(ref builder, dynFmt, ref part);
        return builder.ToStringAndDispose();
    }

    /// <summary> Formats parts to a temporary string via a <see cref="FormatString"/>. </summary>
    internal static TempString FormatTemp<T>(FormatString dynFmt, ref T part) where T : struct, IFormatItem
    {
        // Match NRE behavior of Format, we'd get one later anyways
        ThrowHelper.NullRefIfNull(dynFmt.text);

        ValueStringBuilder builder = new(256);
        WriteTo(ref builder, dynFmt, ref part);
        return builder.ToTempStringAndDispose();
    }

    /// <summary> Formats parts into a span. </summary>
    internal static bool TryFormat<T>(FormatString dynFmt, Span<char> destination, ref T part, out int charsWritten) where T : struct, IFormatItem
    {
        // Match NRE behavior of Format, we'd get one later anyways
        ThrowHelper.NullRefIfNull(dynFmt.text);
        var fmt = dynFmt.text.AsSpan();

        int pos = 0;
        int len = 0;
        foreach (var item in dynFmt.holes)
        {
            var slice = fmt[pos..item.Index];
            if (!slice.TryCopyTo(destination[len..])) goto Fail;
            len += slice.Length;

            if (!part.TryWriteTo(destination[len..], item.Slot, item.Format, out int partLen)) goto Fail;
            len += partLen;
            pos = item.Index;
        }

        var lastSlice = fmt[pos..];
        if (!lastSlice.TryCopyTo(destination[len..])) goto Fail;
        len += lastSlice.Length;

        charsWritten = len;
        return true;

    Fail:
        charsWritten = 0;
        return false;
    }

    /// <summary> Writes parts to a string builder via a <see cref="FormatString"/>. </summary>
    internal static void WriteTo<T>(ref ValueStringBuilder builder, FormatString dynFmt, ref T part) where T : struct, IFormatItem
    {
        // We wouldn't get here if this was null, but we include this check
        // here anyway so the JIT can reduce the amount of generated code.
        ThrowHelper.NullRefIfNull(dynFmt.text);
        var fmt = dynFmt.text.AsSpan();

        int pos = 0;
        foreach (var item in dynFmt.holes)
        {
            builder.Append(fmt[pos..item.Index]);
            part.AppendTo(ref builder, item.Slot, item.Format);
            pos = item.Index;
        }

        builder.Append(fmt[pos..]);
    }

    /// <summary> Writes parts to a string builder via a <see cref="FormatString"/>. </summary>
    internal static StringBuilder WriteTo<T>(StringBuilder builder, FormatString dynFmt, ref T part) where T : struct, IFormatItem
    {
        if (dynFmt.holes.Length == 0)
        {
            return builder.Append(dynFmt.text);
        }

        ThrowHelper.NullRefIfNull(dynFmt.text);
        var fmt = dynFmt.text.AsSpan();

        int pos = 0;
        foreach (var item in dynFmt.holes)
        {
            builder.Append(fmt[pos..item.Index]);
            part.AppendTo(builder, item.Slot, item.Format);
            pos = item.Index;
        }

        return builder.Append(fmt[pos..]);
    }

    /// <summary> Appends a value to a string builder with a format. </summary>
    internal static void AppendFormatted<T>(this StringBuilder builder, T? value, string? format)
    {
#pragma warning disable IDE0038
        if (typeof(T).IsValueType && value is IStringBuilderAppendable)
        {
            ((IStringBuilderAppendable)value).AppendTo(builder, format);
        }
#pragma warning restore IDE0038
        else
        {
            // Essentially: `builder.Append($"{value:format}")`
            // but with the format defined by a variable.
            var handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, builder);
            handler.AppendFormatted(value, format);

            // This `Append` is basically a no-op but we call it to retain expectations.
            builder.Append(ref handler);
        }
    }
}

file interface IFormatItem
{
    void AppendTo(ref ValueStringBuilder builder, int slot, string? format);
    void AppendTo(StringBuilder builder, int slot, string? format);
    bool TryWriteTo(Span<char> destination, int slot, string? format, out int charsWritten);
}

file static class FormatItems
{
    public static void ThrowUnknownSlot()
        => ThrowHelper.Argument("slot", "No value for that slot was defined.");

    public static bool ThrowUnknownSlot(out int charsWritten)
    {
        ThrowUnknownSlot();
        charsWritten = 0;
        return false;
    }

    public static bool TryWriteTo<T>(T value, Span<char> destination, string? format, out int charsWritten)
    {
#pragma warning disable IDE0038
        string? text = null;
        if (value is IFormattable)
        {
            if (value is ISpanFormattable)
            {
                return ((ISpanFormattable)value).TryFormat(destination, out charsWritten, format, provider: null);
            }

            text = ((IFormattable)value).ToString(format, null);
        }
#pragma warning restore IDE0038
        else if (value != null)
        {
            text = value.ToString();
        }

        if (text == null)
        {
            charsWritten = 0;
            return true;
        }

        return FormatUtil.TryWrite(destination, text, out charsWritten);
    }
}

file struct FormatItems<T> : IFormatItem
{
    public T? Item0;

    public readonly void AppendTo(ref ValueStringBuilder builder, int slot, string? format)
    {
        if (slot != 0)
            FormatItems.ThrowUnknownSlot();

        builder.Append(Item0, format);
    }

    public readonly void AppendTo(StringBuilder builder, int slot, string? format)
    {
        if (slot != 0)
            FormatItems.ThrowUnknownSlot();

        builder.AppendFormatted(Item0, format);
    }

    public readonly bool TryWriteTo(Span<char> destination, int slot, string? format, out int charsWritten)
    {
        if (slot != 0)
            FormatItems.ThrowUnknownSlot();

        return FormatItems.TryWriteTo(Item0, destination, format, out charsWritten);
    }
}

file struct FormatItems<T0, T1> : IFormatItem
{
    public T0? Item0;
    public T1? Item1;

    public readonly void AppendTo(ref ValueStringBuilder builder, int slot, string? format)
    {
        switch (slot)
        {
            case 0: builder.Append(Item0, format); break;
            case 1: builder.Append(Item1, format); break;
            default: FormatItems.ThrowUnknownSlot(); break;
        }
    }

    public readonly void AppendTo(StringBuilder builder, int slot, string? format)
    {
        switch (slot)
        {
            case 0: builder.AppendFormatted(Item0, format); break;
            case 1: builder.AppendFormatted(Item1, format); break;
            default: FormatItems.ThrowUnknownSlot(); break;
        }
    }

    public readonly bool TryWriteTo(Span<char> destination, int slot, string? format, out int charsWritten)
    {
        return slot switch
        {
            0 => FormatItems.TryWriteTo(Item0, destination, format, out charsWritten),
            1 => FormatItems.TryWriteTo(Item1, destination, format, out charsWritten),
            _ => FormatItems.ThrowUnknownSlot(out charsWritten),
        };
    }
}

file struct FormatItems<T0, T1, T2> : IFormatItem
{
    public T0? Item0;
    public T1? Item1;
    public T2? Item2;

    public readonly void AppendTo(ref ValueStringBuilder builder, int slot, string? format)
    {
        switch (slot)
        {
            case 0: builder.Append(Item0, format); break;
            case 1: builder.Append(Item1, format); break;
            case 2: builder.Append(Item2, format); break;
            default: FormatItems.ThrowUnknownSlot(); break;
        }
    }

    public readonly void AppendTo(StringBuilder builder, int slot, string? format)
    {
        switch (slot)
        {
            case 0: builder.AppendFormatted(Item0, format); break;
            case 1: builder.AppendFormatted(Item1, format); break;
            case 2: builder.AppendFormatted(Item2, format); break;
            default: FormatItems.ThrowUnknownSlot(); break;
        }
    }

    public readonly bool TryWriteTo(Span<char> destination, int slot, string? format, out int charsWritten)
    {
        return slot switch
        {
            0 => FormatItems.TryWriteTo(Item0, destination, format, out charsWritten),
            1 => FormatItems.TryWriteTo(Item1, destination, format, out charsWritten),
            2 => FormatItems.TryWriteTo(Item2, destination, format, out charsWritten),
            _ => FormatItems.ThrowUnknownSlot(out charsWritten),
        };
    }
}

file struct FormatItems<T0, T1, T2, T3> : IFormatItem
{
    public T0? Item0;
    public T1? Item1;
    public T2? Item2;
    public T3? Item3;

    public readonly void AppendTo(ref ValueStringBuilder builder, int slot, string? format)
    {
        switch (slot)
        {
            case 0: builder.Append(Item0, format); break;
            case 1: builder.Append(Item1, format); break;
            case 2: builder.Append(Item2, format); break;
            case 3: builder.Append(Item3, format); break;
            default: FormatItems.ThrowUnknownSlot(); break;
        }
    }

    public readonly void AppendTo(StringBuilder builder, int slot, string? format)
    {
        switch (slot)
        {
            case 0: builder.AppendFormatted(Item0, format); break;
            case 1: builder.AppendFormatted(Item1, format); break;
            case 2: builder.AppendFormatted(Item2, format); break;
            case 3: builder.AppendFormatted(Item3, format); break;
            default: FormatItems.ThrowUnknownSlot(); break;
        }
    }

    public readonly bool TryWriteTo(Span<char> destination, int slot, string? format, out int charsWritten)
    {
        return slot switch
        {
            0 => FormatItems.TryWriteTo(Item0, destination, format, out charsWritten),
            1 => FormatItems.TryWriteTo(Item1, destination, format, out charsWritten),
            2 => FormatItems.TryWriteTo(Item2, destination, format, out charsWritten),
            3 => FormatItems.TryWriteTo(Item3, destination, format, out charsWritten),
            _ => FormatItems.ThrowUnknownSlot(out charsWritten),
        };
    }
}

file readonly struct FormatItemsVarArgs<T> : IFormatItem
{
    private readonly T[] _args;

    public FormatItemsVarArgs(T[] args) => _args = args;

    public void AppendTo(ref ValueStringBuilder builder, int slot, string? format)
    {
        var args = _args;
        if ((uint)slot >= (uint)_args.Length)
            FormatItems.ThrowUnknownSlot();

        builder.Append(args[slot], format);
    }

    public void AppendTo(StringBuilder builder, int slot, string? format)
    {
        var args = _args;
        if ((uint)slot >= (uint)_args.Length)
            FormatItems.ThrowUnknownSlot();

        builder.AppendFormatted(args[slot], format);
    }

    public bool TryWriteTo(Span<char> destination, int slot, string? format, out int charsWritten)
    {
        var args = _args;
        if ((uint)slot >= (uint)_args.Length)
            FormatItems.ThrowUnknownSlot();

        return FormatItems.TryWriteTo(args, destination, format, out charsWritten);
    }
}

file struct Marker : IFormatItem
{
    public ValueList<FormatHole> Holes;

    public void AppendTo(ref ValueStringBuilder builder, int slot, string? format)
    {
        Holes.Add(new FormatHole(builder.Length, slot, format));
    }

    void IFormatItem.AppendTo(StringBuilder builder, int slot, string? format)
    {
        throw new NotSupportedException();
    }

    bool IFormatItem.TryWriteTo(Span<char> destination, int slot, string? format, out int charsWritten)
    {
        throw new NotSupportedException();
    }
}
