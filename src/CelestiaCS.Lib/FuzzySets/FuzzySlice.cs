using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CelestiaCS.Lib.FuzzySets;

/// <summary>
/// Represents an array-based slice of a UTF-16 string.
/// </summary>
[SkipLocalsInit]
internal readonly struct FuzzySlice : IEquatable<FuzzySlice>
{
    private readonly char[] _buffer;
    private readonly int _length;

    public FuzzySlice(char[] buffer)
    {
        _buffer = buffer;
        _length = buffer.Length;
    }

    public FuzzySlice(char[] buffer, int length)
    {
        if ((uint)length > (uint)buffer.Length)
            ThrowHelper.ArgumentOutOfRange(nameof(length));

        _buffer = buffer;
        _length = length;
    }

    public static FuzzySlice Empty => new([]);

    public char[] Buffer => _buffer;
    public int Length => _length;

    internal bool IsValid => _buffer is { } b && (uint)_length <= (uint)b.Length;

    public char[] ToArray() => AsSpan().ToArray();

    public ReadOnlySpan<char> AsSpan()
    {
        char[] buffer = _buffer;
        int length = _length;

        // We want a bit of safety here in case it does get torn
        if ((uint)length > (uint)buffer.Length)
            ThrowHelper.InvalidOperation();

        return MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(buffer), length);
    }

    public FuzzySlice Copy() => new(ToArray());

    public override bool Equals(object? obj) => obj is FuzzySlice slice && Equals(slice);

    public bool Equals(FuzzySlice other)
    {
        return AsSpan().SequenceEqual(other.AsSpan());
    }

    public override int GetHashCode()
    {
        HashCode hashCode = default;
        hashCode.Add(Length);
        hashCode.AddBytes(MemoryMarshal.AsBytes(AsSpan()));
        return hashCode.ToHashCode();
    }

    public override string ToString() => new string(_buffer);

    public static bool operator ==(FuzzySlice left, FuzzySlice right) => left.Equals(right);
    public static bool operator !=(FuzzySlice left, FuzzySlice right) => !left.Equals(right);
}
