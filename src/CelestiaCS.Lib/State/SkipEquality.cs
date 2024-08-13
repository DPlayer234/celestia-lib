using System;
using System.Diagnostics.CodeAnalysis;

namespace CelestiaCS.Lib.State;

#pragma warning disable CA2231 // Overload operator equals on overriding value type Equals

/// <summary>
/// Represents a field of type <typeparamref name="T"/> that does not contribute to record equality.
/// </summary>
/// <remarks>
/// The Equals methods have been overridden to always return <see langword="true"/> if the other parameter is also <see cref="SkipEquality{T}"/>.
/// </remarks>
/// <typeparam name="T"> The element type. </typeparam>
/// <param name="value"> The value. </param>
public readonly struct SkipEquality<T>(T value) : IEquatable<SkipEquality<T>>
{
    /// <summary> The value. </summary>
    public T Value => value;

    /// <summary> Always returns <see langword="true"/>. </summary>
    public bool Equals(SkipEquality<T> other) => true;
    /// <summary> Returns <see langword="true"/> if <paramref name="obj"/> is a <see cref="SkipEquality{T}"/>. </summary>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is SkipEquality<T>;
    /// <summary> Always returns the same value per program instance. </summary>
    public override int GetHashCode() => 0;

    public static implicit operator T(SkipEquality<T> value) => value.Value;
    public static implicit operator SkipEquality<T>(T value) => new(value);
}
