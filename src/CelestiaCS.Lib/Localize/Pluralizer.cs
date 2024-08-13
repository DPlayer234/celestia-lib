using System;
using CelestiaCS.Lib.Format;

namespace CelestiaCS.Lib.Localize;

/// <summary>
/// Allows interpolating a numeric value as both a number and a selector for the count-ness of something.
/// </summary>
/// <param name="count"> The count to use. </param>
public readonly struct Pluralizer(int count) : ISpanFormattable
{
    string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
    {
        ValueStringBuilder result = new(stackalloc char[64]);
        result.Append(this, format);
        return result.ToStringAndDispose();
    }

    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        // While custom number format strings can also separate positive/negative/zero with semicolons,
        // we simply consider this to not be meaningful here because a count should be zero or positive.
        int semicolon = format.IndexOf(';');
        if (semicolon < 0)
        {
            return count.TryFormat(destination, out charsWritten, format, provider);
        }

        // CMBK: Handle languages that don't just have one/many forms
        format = count == 1
            ? format[..semicolon]
            : format[(semicolon + 1)..];

        if (format.TryCopyTo(destination))
        {
            charsWritten = format.Length;
            return true;
        }

        charsWritten = 0;
        return false;
    }
}
