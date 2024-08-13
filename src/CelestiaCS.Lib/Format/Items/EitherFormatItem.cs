using System;
using System.Runtime.InteropServices;
using CelestiaCS.Lib.State;

namespace CelestiaCS.Lib.Format.Items;

/// <summary>
/// Holds one of two kinds of values with a predefined format string.
/// </summary>
/// <typeparam name="T1"> The first possible type. </typeparam>
/// <typeparam name="T2"> The second possible type. </typeparam>
[StructLayout(LayoutKind.Auto)]
public readonly struct EitherFormatItem<T1, T2> : ISpanFormattable
    where T1 : notnull, ISpanFormattable
    where T2 : notnull, ISpanFormattable
{
    private readonly Either<T1, T2> _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="EitherFormatItem{T1, T2}"/> struct.
    /// </summary>
    /// <param name="value"> The value to format. </param>
    /// <param name="format"> The format string to use. </param>
    public EitherFormatItem(Either<T1, T2> value, string? format = null)
    {
        _value = value;
        Format = format;
    }

    /// <summary> The value to format. </summary>
    public Either<T1, T2> Value => _value;

    /// <summary> The format string to use. </summary>
    public string? Format { get; }

    /// <inheritdoc cref="IFormattable.ToString(string?, IFormatProvider?)"/>
    public override string ToString()
        => ToString(null, null);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider)
        => _value.TryGet(out T1? t1) ? t1.ToString(Format, formatProvider)
        : _value.TryGet(out T2? t2) ? t2.ToString(Format, formatProvider)
        : string.Empty;

    /// <inheritdoc/>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        => _value.TryGet(out T1? t1) ? t1.TryFormat(destination, out charsWritten, Format, provider)
        : _value.TryGet(out T2? t2) ? t2.TryFormat(destination, out charsWritten, Format, provider)
        : Empty(out charsWritten);

    private static bool Empty(out int charsWritten)
    {
        charsWritten = 0;
        return true;
    }
}
