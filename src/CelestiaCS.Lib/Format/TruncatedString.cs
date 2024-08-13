using System;

namespace CelestiaCS.Lib.Format;

/// <summary>
/// Represents a string that may be truncated and is supposed to be interpolated into a string.
/// </summary>
public readonly struct TruncatedString : ISpanFormattable
{
    private readonly string? _source;
    private readonly int _maxLength;
    private readonly string? _truncation;

    internal TruncatedString(string source, int maxLength, string? truncation)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

        if (truncation != null && truncation.Length > maxLength)
            ThrowHelper.Argument(nameof(truncation), "Truncation must not be longer than maxLength.");

        _source = source;
        _maxLength = maxLength;
        _truncation = truncation;
    }

    /// <summary>
    /// Tries to format this instance into a span.
    /// </summary>
    /// <param name="destination"> The destination span. </param>
    /// <param name="charsWritten"> The amount of characters written. </param>
    /// <returns> Whether the operation was successful. </returns>
    public bool TryFormat(Span<char> destination, out int charsWritten)
    {
        string? source = _source;
        if (source == null)
        {
            charsWritten = 0;
            return true;
        }

        int maxLength = _maxLength;
        if (source.Length <= maxLength)
        {
            charsWritten = source.Length;
            return source.TryCopyTo(destination);
        }

        if (destination.Length < maxLength)
        {
            charsWritten = 0;
            return false;
        }

        charsWritten = maxLength;

        string? truncation = _truncation;
        if (string.IsNullOrEmpty(truncation))
        {
            return source.AsSpan(0, maxLength).TryCopyTo(destination);
        }

        int mainLength = maxLength - truncation.Length;
        return source.AsSpan(0, mainLength).TryCopyTo(destination)
            && truncation.TryCopyTo(destination[mainLength..]);
    }

    /// <summary>
    /// Converts this instance into a string.
    /// </summary>
    public override string ToString()
    {
        string? source = _source;
        if (source == null)
            return string.Empty;

        int maxLength = _maxLength;
        if (source.Length <= maxLength)
            return source;

        string? truncation = _truncation;
        if (string.IsNullOrEmpty(truncation))
            return source[..maxLength];

        return string.Create(maxLength, (s: source, t: truncation), static (span, p) =>
        {
            int mainLength = span.Length - p.t.Length;
            p.s.AsSpan(0, mainLength).CopyTo(span);
            p.t.CopyTo(span[mainLength..]);
        });
    }

    public static implicit operator string(TruncatedString truncated) => truncated.ToString();

    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString();
    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => TryFormat(destination, out charsWritten);
}
