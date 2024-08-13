using System;
using System.Buffers;

namespace CelestiaCS.Lib.Format;

/// <summary>
/// A string that has been markdown escaped.
/// </summary>
public readonly struct MdEscapedString : ISpanFormattable
{
    private readonly string? _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="MdEscapedString"/> struct from a non-escaped string.
    /// </summary>
    /// <param name="value"> The string to escape. </param>
    public MdEscapedString(string? value) => _value = value;

    /// <summary>
    /// Converts this instance into a proper string.
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
        return MdEscapedStringCore.TryFormat(_value, destination, out charsWritten);
    }

    /// <summary>
    /// Tries to escape <paramref name="source"/> into a span.
    /// </summary>
    /// <param name="source"> The source span to escape. </param>
    /// <param name="destination"> The destination span. </param>
    /// <param name="charsWritten"> The amount of characters written. </param>
    /// <returns> Whether the operation was successful. </returns>
    public static bool TryFormat(ReadOnlySpan<char> source, Span<char> destination, out int charsWritten)
    {
        return MdEscapedStringCore.TryFormat(source, destination, out charsWritten);
    }

    public static implicit operator string(MdEscapedString value) => value.ToString();

    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString();
    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => TryFormat(destination, out charsWritten);
}

file static class MdEscapedStringCore
{
    private static readonly SearchValues<char> _mdEscapeNeeded = SearchValues.Create("_*~|`<>\\@");

    private const char InvisChar = '\u2063';
    private const char MdEscape = '\\';

    public static bool TryFormat(ReadOnlySpan<char> source, Span<char> destination, out int charsWritten)
    {
        if (destination.Length < source.Length)
            goto Fail;

        int written = 0;
        while (!source.IsEmpty)
        {
            // Find the next character that needs an escape.
            int index = source.IndexOfAny(_mdEscapeNeeded);
            if (index < 0)
            {
                // None found: Copy the rest to the destination.
                if (!source.TryCopyTo(destination))
                    goto Fail;

                written += source.Length;
                goto Success;
            }

            // The next sequence will always be
            // * characters before the one found
            // * the character to escape
            // * one escape character
            // so we prepend a check for that.
            int cpyLen = index + 2;
            if ((uint)destination.Length < (uint)cpyLen)
                goto Fail;

            // Copy the relevant slice.
            char mdChar = source[index];
            source[..index].CopyTo(destination);

            // Pick the appropriate way to escape and write the characters
            var (f, s) = mdChar == '@' ? (mdChar, InvisChar) : (MdEscape, mdChar);

            destination[index + 1] = s;
            destination[index] = f;

            // Slice input and output for the next iteration
            source = source[(index + 1)..];
            destination = destination[cpyLen..];

            written += cpyLen;
        }

    Success:
        charsWritten = written;
        return true;

    Fail:
        charsWritten = 0;
        return false;
    }
}
