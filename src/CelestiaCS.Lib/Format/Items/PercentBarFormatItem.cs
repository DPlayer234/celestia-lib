using System;
using System.Diagnostics;

namespace CelestiaCS.Lib.Format.Items;

public readonly struct PercentBarFormatItem : ISpanFormattable
{
    private readonly int _width;
    private readonly float _percent;

    public PercentBarFormatItem(int width, float percent)
    {
        _width = width;
        _percent = percent;
    }

    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => $"{this}";

    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        int width = _width;
        if (destination.Length < width)
        {
            goto Fail;
        }

        float percent = _percent;
        Span<char> num = stackalloc char[10];
        bool ok = Math.Clamp((int)(percent * 100), min: -999, max: 999).TryFormat(num, out int numWidth);
        Debug.Assert(ok);

        if (!FormatUtil.BarRel('=', '\u00a0', width, percent).TryFormat(destination, out _))
        {
            goto Fail;
        }

        int index = width / 2 + 1;
        num[..numWidth].CopyTo(destination[(index - numWidth)..]);
        destination[index] = '%';

        charsWritten = width;
        return true;

    Fail:
        charsWritten = 0;
        return false;
    }
}
