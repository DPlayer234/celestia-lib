using System;
using System.Runtime.InteropServices;

namespace CelestiaCS.Lib.Format.Items;

/// <summary>
/// An interpolatable value that repeats the given value a specified amount of times.
/// </summary>
/// <typeparam name="T"> The type of the value. </typeparam>
[StructLayout(LayoutKind.Auto)]
public readonly struct RepeatFormatItem<T> : ISpanFormattable
{
    private readonly T? _value;
    private readonly int _count;

    /// <summary>
    /// Initializes a new instance of the <see cref="RepeatFormatItem{T}"/> struct.
    /// </summary>
    /// <param name="value"> The value to write. </param>
    /// <param name="count"> The repeat count. </param>
    public RepeatFormatItem(T? value, int count)
    {
        _value = value;
        _count = count;
    }

    /// <summary>
    /// Returns the joined string.
    /// </summary>
    public override string ToString() => $"{this}";

    /// <summary>
    /// Tries to format this instance into a span.
    /// </summary>
    /// <param name="destination"> The destination span. </param>
    /// <param name="charsWritten"> The amount of characters written. </param>
    /// <param name="provider"> The format provider. </param>
    /// <returns> Whether the operation was successful. </returns>
    public bool TryFormat(Span<char> destination, out int charsWritten)
    {
        int count = _count;
        if (count <= 0)
        {
            charsWritten = 0;
            goto Success;
        }

        if (typeof(T) == typeof(char))
        {
            // Special case for char as we can just use Span<T>.Fill
            if (destination.Length < count)
                goto Fail;

            destination[..count].Fill((char)(object)_value!);
            charsWritten = count;
        }
        else
        {
            if (!destination.TryWrite($"{_value}", out int wri))
                goto Fail;

            int totalLen = wri * count;
            if (destination.Length < totalLen)
                goto Fail;

            var valueText = destination[..wri];
            for (int i = 1; i < count; i++)
            {
                valueText.CopyTo(destination[(i * wri)..]);
            }

            charsWritten = totalLen;
        }

    Success:
        return true;

    Fail:
        charsWritten = 0;
        return false;
    }

    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString();
    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => TryFormat(destination, out charsWritten);
}
