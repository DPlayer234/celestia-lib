using System;
using System.Diagnostics;

namespace CelestiaCS.Lib.Format;

/// <summary>
/// Represents a number to be formatted as a roman numeral.
/// </summary>
public readonly struct RomanNumeral : ISpanFormattable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RomanNumeral"/> struct.
    /// </summary>
    /// <param name="value"> The value to format. </param>
    public RomanNumeral(int value) => Value = value;

    /// <summary> The value to format. </summary>
    public int Value { get; }

    /// <summary>
    /// Creates a string that represents the specified number as a roman numeral.
    /// </summary>
    public override string ToString() => RomanNumerals.ToString(Value);

    /// <summary>
    /// Tries to write a string that represents the specified number as a roman numeral to the <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination"> The destination to write the string to. </param>
    /// <param name="charsWritten"> On return, how many characters were written. </param>
    /// <returns> If the string fit into the <paramref name="destination"/>. </returns>
    public bool TryFormat(Span<char> destination, out int charsWritten) => RomanNumerals.TryFormat(destination, Value, out charsWritten);

    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString();
    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => TryFormat(destination, out charsWritten);
}

/// <summary>
/// Utility to format numbers as roman numerals. The supported numeric range is limited.
/// </summary>
public static class RomanNumerals
{
    public static int MinSupported => -3888;
    public static int MaxSupported => 3888;
    public static int MaxStringSize => 24;

    /// <summary>
    /// Creates a string that represents the specified number as a roman numeral.
    /// </summary>
    /// <param name="value"> The value to format. </param>
    /// <returns> The string containing the representation of the <paramref name="value"/>. </returns>
    public static string ToString(int value)
    {
        Span<char> str = stackalloc char[MaxStringSize];
        bool ok = TryFormat(str, value, out int written);
        Debug.Assert(ok);

        return new string(str[..written]);
    }

    /// <summary>
    /// Returns a value that can be interpolated to create a roman numeral.
    /// </summary>
    /// <param name="value"> The value to format. </param>
    /// <returns> A value that can be interpolated. </returns>
    public static RomanNumeral Interpolate(int value) => new(value);

    /// <summary>
    /// Tries to write a string that represents the specified number as a roman numeral to the <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination"> The destination to write the string to. </param>
    /// <param name="value"> The value to format. </param>
    /// <param name="charsWritten"> On return, how many characters were written. </param>
    /// <returns> If the string fit into the <paramref name="destination"/>. </returns>
    public static bool TryFormat(Span<char> destination, int value, out int charsWritten)
    {
        if (value == 0)
        {
            if (destination.Length < 1)
                return Fail(out charsWritten);

            destination[0] = 'Z';
            charsWritten = 0;
            return true;
        }

        uint v = value > 0 ? (uint)value : (uint)-value;
        uint written = 0;

        AssertSupported(v);

        if (value < 0)
        {
            if (destination.Length < 1)
                return Fail(out charsWritten);

            destination[0] = '-';
            written += 1;
            destination = destination[1..];
        }

        if (v >= 1000)
        {
            uint m = v / 1000;
            if (destination.Length < m)
                return Fail(out charsWritten);

            for (int i = 0; i < m; i++) destination[i] = 'M';
            written += m;

            v -= m * 1000;
            destination = destination[(int)m..];
        }

        if (v >= 900)
        {
            if (destination.Length < 2)
                return Fail(out charsWritten);

            destination[0] = 'C';
            destination[1] = 'M';
            written += 2;

            v -= 900;
            destination = destination[2..];
        }
        else if (v >= 500)
        {
            if (destination.Length < 1)
                return Fail(out charsWritten);

            destination[0] = 'D';
            written += 1;

            v -= 500;
            destination = destination[1..];
        }

        if (v >= 400)
        {
            if (destination.Length < 2)
                return Fail(out charsWritten);

            destination[0] = 'C';
            destination[1] = 'D';
            written += 2;

            v -= 400;
            destination = destination[2..];
        }
        else if (v >= 100)
        {
            uint c = v / 100;
            if (destination.Length < c)
                return Fail(out charsWritten);

            for (int i = 0; (uint)i < c; i++) destination[i] = 'C';
            written += c;

            v -= c * 100;
            destination = destination[(int)c..];
        }

        if (v >= 90)
        {
            if (destination.Length < 2)
                return Fail(out charsWritten);

            destination[0] = 'X';
            destination[1] = 'C';
            written += 2;

            v -= 90;
            destination = destination[2..];
        }
        else if (v >= 50)
        {
            if (destination.Length < 1)
                return Fail(out charsWritten);

            destination[0] = 'L';
            written += 1;

            v -= 50;
            destination = destination[1..];
        }

        if (v >= 40)
        {
            if (destination.Length < 2)
                return Fail(out charsWritten);

            destination[0] = 'X';
            destination[1] = 'L';
            written += 2;

            v -= 40;
            destination = destination[2..];
        }
        else if (v >= 10)
        {
            uint x = v / 10;
            if (destination.Length < x)
                return Fail(out charsWritten);

            for (int i = 0; i < x; i++) destination[i] = 'X';
            written += x;

            v -= x * 10;
            destination = destination[(int)x..];
        }

        if (v >= 9)
        {
            if (destination.Length < 2)
                return Fail(out charsWritten);

            destination[0] = 'I';
            destination[1] = 'X';
            written += 2;

            v -= 9;
            destination = destination[2..];
        }
        else if (v >= 5)
        {
            if (destination.Length < 1)
                return Fail(out charsWritten);

            destination[0] = 'V';
            written += 1;

            v -= 5;
            destination = destination[1..];
        }

        if (v >= 4)
        {
            if (destination.Length < 2)
                return Fail(out charsWritten);

            destination[0] = 'I';
            destination[1] = 'V';

            written += 2;
            //destination = destination[2..];
        }
        else if (v >= 1)
        {
            if (destination.Length < v)
                return Fail(out charsWritten);

            for (int i = 0; i < v; i++) destination[i] = 'I';
            written += v;

            //v = 0;
        }

        charsWritten = (int)written;
        return true;

        static bool Fail(out int charsWritten)
        {
            charsWritten = 0;
            return false;
        }
    }

    private static void AssertSupported(uint value)
    {
        if (value > 3888)
        {
            ThrowHelper.ArgumentOutOfRange(nameof(value), value);
        }
    }
}
