using System;
using System.Diagnostics.CodeAnalysis;

namespace CelestiaCS.Lib.Format;

/// <summary>
/// A helper struct for writing <seealso cref="ISpanFormattable"/> implementations.
/// </summary>
public ref struct SpanFormatWriter
{
    private Span<char> _buffer;

    [SuppressMessage("CodeQuality", "IDE0052", Justification = "Ref member that is written to.")]
    private readonly ref int _charsWritten;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpanFormatWriter"/> struct.
    /// </summary>
    /// <remarks>
    /// Pass in <paramref name="destination"/> and <paramref name="charsWritten"/> from the parameters of <seealso cref="ISpanFormattable"/>.
    /// </remarks>
    /// <param name="destination"> The buffer to write to. </param>
    /// <param name="charsWritten"> The output variable for the amount of characters written. </param>
    public SpanFormatWriter(Span<char> destination, [UnscopedRef] out int charsWritten)
    {
        charsWritten = 0;

        _buffer = destination;
        _charsWritten = ref charsWritten;
    }

    /// <summary>
    /// Appends a string to the buffer.
    /// </summary>
    /// <param name="value"> The string to append. </param>
    /// <returns> Whether it worked. </returns>
    public bool Append(string? value)
    {
        if (value == null) return true;

        var buffer = _buffer;
        if (value.TryCopyTo(buffer))
        {
            int length = value.Length;
            _buffer = buffer[length..];
            _charsWritten += length;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Appends a character to the buffer.
    /// </summary>
    /// <param name="value"> The character to append. </param>
    /// <returns> Whether it worked. </returns>
    public bool Append(char value)
    {
        var buffer = _buffer;
        if (buffer.Length > 0)
        {
            buffer[0] = value;
            _buffer = buffer[1..];
            _charsWritten += 1;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Appends a value to the buffer.
    /// </summary>
    /// <remarks>
    /// If the <paramref name="value"/> implements <seealso cref="ISpanFormattable"/>, this method delegates to that implementation.
    /// </remarks>
    /// <typeparam name="T"> The type of the value. </typeparam>
    /// <param name="value"> The value to append. </param>
    /// <returns> Whether it worked. </returns>
    public bool Append<T>(T? value)
    {
#pragma warning disable IDE0038
        if (value is ISpanFormattable)
        {
            var buffer = _buffer;
            if (((ISpanFormattable)value).TryFormat(buffer, out int lCharsWritten, default, null))
            {
                _buffer = buffer[lCharsWritten..];
                _charsWritten += lCharsWritten;
                return true;
            }

            return false;
        }
#pragma warning restore IDE0038
        else
        {
            return value is null
                || Append(value.ToString());
        }
    }

    /// <summary>
    /// Appends a formattable value to the buffer.
    /// </summary>
    /// <typeparam name="T"> The type of the value. </typeparam>
    /// <param name="value"> The value to append. </param>
    /// <param name="format"> The format to use. </param>
    /// <returns> Whether it worked. </returns>
    public bool Append<T>(T? value, string? format) where T : ISpanFormattable
    {
        return value is null
            || Append(FormatUtil.With(value, format));
    }

    /// <summary>
    /// Appends a string to the buffer.
    /// </summary>
    /// <param name="value"> The string to append. </param>
    /// <returns> Whether it worked. </returns>
    public bool Append(ReadOnlySpan<char> value)
    {
        var buffer = _buffer;
        if (value.TryCopyTo(buffer))
        {
            int length = value.Length;
            _buffer = buffer[length..];
            _charsWritten += length;

            return true;
        }

        return false;
    }
}
