using System;
using System.Runtime.InteropServices;

namespace CelestiaCS.Lib.Format.Items;

/// <summary>
/// Holds a value with predefined format string.
/// </summary>
/// <typeparam name="T"> The type of the value. </typeparam>
/// <param name="value"> The value to format. </param>
/// <param name="format"> The format string to use. </param>
[StructLayout(LayoutKind.Auto)]
public readonly struct FormatItem<T>(T value, string? format = null) : ISpanFormattable
    where T : ISpanFormattable
{
    /// <summary> The value to format. </summary>
    public T Value { get; } = value;

    /// <summary> The format string to use. </summary>
    public string? Format { get; } = format;

    /// <inheritdoc cref="IFormattable.ToString(string?, IFormatProvider?)"/>
    public override string ToString()
        => ToString(null, null);

#pragma warning disable CL0004 // Mutable call on property value

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider)
        => Value?.ToString(Format, formatProvider) ?? string.Empty;

    /// <inheritdoc/>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        => Value?.TryFormat(destination, out charsWritten, Format, provider) ?? SuccessOnNull(out charsWritten);

    private static bool SuccessOnNull(out int charsWritten)
    {
        charsWritten = 0;
        return true;
    }
}
