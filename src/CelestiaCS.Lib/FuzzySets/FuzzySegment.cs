using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CelestiaCS.Lib.Dangerous;
using CelestiaCS.Lib.Memory;

namespace CelestiaCS.Lib.FuzzySets;

/// <summary>
/// Represents a value-type string segment used by the <see cref="FuzzySet{TMeta}"/> implementation.
/// Contains at most 8 characters.
/// </summary>
[SkipLocalsInit]
internal readonly struct FuzzySegment : IEquatable<FuzzySegment>
{
    public const int MaxContentLength = 8; // sizeof(UInt128) / sizeof(char)

    private readonly UInt128 _value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FuzzySegment(ReadOnlySpan<char> buffer)
    {
        Debug.Assert(buffer.Length <= MaxContentLength);

        _value = default;
        buffer.CopyTo(DangerousSpan.CreateFromBuffer<UInt128, char>(ref _value));
    }

    public UInt128 Value => _value;

    [UnscopedRef]
    public ReadOnlySpan<char> Text => DangerousSpan.CreateFromReadOnlyBuffer<UInt128, char>(in _value).NullTerminate();

    public override bool Equals(object? obj) => obj is FuzzySegment g && Equals(g);
    public override int GetHashCode() => _value.GetHashCode();

    public bool Equals(FuzzySegment other) => _value == other._value;

    public override string ToString() => new string(Text);

    public static bool operator ==(FuzzySegment lhs, FuzzySegment rhs) => lhs.Equals(rhs);
    public static bool operator !=(FuzzySegment lhs, FuzzySegment rhs) => !lhs.Equals(rhs);
}
