using System;
using System.Runtime.InteropServices;

namespace CelestiaCS.Lib.Format.Items;

[StructLayout(LayoutKind.Auto)]
public readonly struct BarFormatItem<T> : ISpanFormattable
{
    private readonly T? _filled;
    private readonly T? _missing;
    private readonly ushort _fillCount;
    private readonly ushort _missingCount;

    public BarFormatItem(T? filled, T? missing, ushort fillCount, ushort missingCount)
    {
        _filled = filled;
        _missing = missing;
        _fillCount = fillCount;
        _missingCount = missingCount;
    }

    public BarFormatItem<T> Reverse()
    {
        return new(_missing, _filled, _missingCount, _fillCount);
    }

    public bool TryFormat(Span<char> destination, out int charsWritten)
    {
        if (typeof(T) == typeof(char))
        {
            // Special case for char as we can just use Span<T>.Fill directly.
            // The repeat-item does so too, but this way we can short-circuit
            // failure with just a single check.
            int fillCount = _fillCount;
            int missingCount = _missingCount;
            int total = fillCount + missingCount;

            if (destination.Length < total)
                goto Fail;

            charsWritten = total;
            destination[..fillCount].Fill((char)(object)_filled!);
            destination.Slice(fillCount, missingCount).Fill((char)(object)_missing!);
            return true;
        }
        else
        {
            if (FormatUtil.Repeat(_filled, _fillCount).TryFormat(destination, out int w1) &&
                FormatUtil.Repeat(_missing, _missingCount).TryFormat(destination[w1..], out int w2))
            {
                charsWritten = w1 + w2;
                return true;
            }

            goto Fail;
        }

    Fail:
        charsWritten = 0;
        return false;
    }

    public override string ToString() => $"{this}";

    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString();

    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => TryFormat(destination, out charsWritten);
}
