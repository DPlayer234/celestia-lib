using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace CelestiaCS.Lib.State;

/// <summary>
/// Represents one of two values in a type-safe way.
/// </summary>
/// <typeparam name="T1"> The first possible type. </typeparam>
/// <typeparam name="T2"> The second possible type. </typeparam>
[StructLayout(LayoutKind.Auto)]
[JsonConverter(typeof(EitherJsonConverterFactory))]
[DebuggerDisplay("{DebugValue}")]
public readonly struct Either<T1, T2> : IEquatable<Either<T1, T2>>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly byte _slot;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly T1? _t1;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly T2? _t2;

    /// <summary>
    /// Creates a new instance with the given value.
    /// </summary>
    /// <param name="t1"> A value to store. </param>
    public Either(T1 t1)
    {
        _slot = 1;
        _t1 = t1;
        _t2 = default;
    }

    /// <summary>
    /// Creates a new instance with the given value.
    /// </summary>
    /// <param name="t2"> A value to store. </param>
    public Either(T2 t2)
    {
        _slot = 2;
        _t1 = default;
        _t2 = t2;
    }

    /// <summary> Gets an instance that holds no value. </summary>
    public static Either<T1, T2> None => default;

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    private object? DebugValue => _slot switch
    {
        1 => _t1,
        2 => _t2,
        _ => NoneDebuggerValue.instance
    };

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is Either<T1, T2> a ? Equals(a)
            : obj is Either<T2, T1> b && Equals(b);
    }

    /// <inheritdoc/>
    public bool Equals(Either<T1, T2> other)
    {
        byte slot = _slot;
        return slot == other._slot && slot switch
        {
            0 => true,
            1 => EqualityComparer<T1>.Default.Equals(_t1, other._t1),
            2 => EqualityComparer<T2>.Default.Equals(_t2, other._t2),
            _ => (bool)ThrowHelper.InvalidOperationReturn()
        };
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return _slot switch
        {
            0 => 0,
            1 => _t1?.GetHashCode() ?? 0,
            2 => _t2?.GetHashCode() ?? 0,
            _ => (int)ThrowHelper.InvalidOperationReturn()
        };
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return _slot switch
        {
            0 => "<Empty>",
            1 => _t1?.ToString() ?? string.Empty,
            2 => _t2?.ToString() ?? string.Empty,
            _ => (string)ThrowHelper.InvalidOperationReturn()
        };
    }

    /// <summary>
    /// Tries to get the value of the first type out.
    /// </summary>
    /// <param name="t1"> The value, if it was stored. </param>
    /// <returns> If the value was retrieved. </returns>
    public bool TryGet([MaybeNullWhen(false)] out T1 t1)
    {
        t1 = _t1;
        return _slot == 1;
    }

    /// <summary>
    /// Tries to get the value of the second type out.
    /// </summary>
    /// <param name="t2"> The value, if it was stored. </param>
    /// <returns> If the value was retrieved. </returns>
    public bool TryGet([MaybeNullWhen(false)] out T2 t2)
    {
        t2 = _t2;
        return _slot == 2;
    }

    /// <summary>
    /// Creates a new instance with the type parameters flipped, but representing the same value.
    /// </summary>
    /// <param name="v"> The original value. </param>
    public static implicit operator Either<T2, T1>(Either<T1, T2> v)
    {
        return v._slot switch
        {
            0 => default,
            1 => new Either<T2, T1>(v._t1!),
            2 => new Either<T2, T1>(v._t2!),
            _ => (Either<T2, T1>)ThrowHelper.InvalidOperationReturn()
        };
    }

    /// <summary>
    /// Creates a new <see cref="Either{T1, T2}"/> with the given value.
    /// </summary>
    /// <param name="t1"> The value. </param>
    public static implicit operator Either<T1, T2>(T1 t1)
        => new Either<T1, T2>(t1);

    /// <summary>
    /// Creates a new <see cref="Either{T1, T2}"/> with the given value.
    /// </summary>
    /// <param name="t2"> The value. </param>
    public static implicit operator Either<T1, T2>(T2 t2)
        => new Either<T1, T2>(t2);

    /// <summary>
    /// Extracts a value of type <typeparamref name="T1"/> out of this instance.
    /// </summary>
    /// <param name="v"> The value. </param>
    /// <exception cref="InvalidCastException"> The held value is not a <typeparamref name="T1"/>. </exception>
    public static explicit operator T1(Either<T1, T2> v)
    {
        if (v._slot != 1)
            ThrowHelper.InvalidCast();

        return v._t1!;
    }

    /// <summary>
    /// Extracts a value of type <typeparamref name="T2"/> out of this instance.
    /// </summary>
    /// <param name="v"> The value. </param>
    /// <exception cref="InvalidCastException"> The held value is not a <typeparamref name="T2"/>. </exception>
    public static explicit operator T2(Either<T1, T2> v)
    {
        if (v._slot != 2)
            ThrowHelper.InvalidCast();

        return v._t2!;
    }

    public static bool operator ==(Either<T1, T2> left, Either<T1, T2> right)
        => left.Equals(right);

    public static bool operator !=(Either<T1, T2> left, Either<T1, T2> right)
        => !left.Equals(right);

    #region Equality directly with T1/T2

    #region == / != T1

    public static bool operator ==(Either<T1, T2> left, T1 right)
        => left._slot == 1 && EqualityComparer<T1>.Default.Equals(left._t1, right);

    public static bool operator !=(Either<T1, T2> left, T1 right)
        => !(left == right);

    public static bool operator ==(T1 left, Either<T1, T2> right)
        => right._slot == 1 && EqualityComparer<T1>.Default.Equals(left, right._t1);

    public static bool operator !=(T1 left, Either<T1, T2> right)
        => !(left == right);

    #endregion

    #region == / != T2

    public static bool operator ==(Either<T1, T2> left, T2 right)
        => left._slot == 2 && EqualityComparer<T2>.Default.Equals(left._t2, right);

    public static bool operator !=(Either<T1, T2> left, T2 right)
        => !(left == right);

    public static bool operator ==(T2 left, Either<T1, T2> right)
        => right._slot == 2 && EqualityComparer<T2>.Default.Equals(left, right._t2);

    public static bool operator !=(T2 left, Either<T1, T2> right)
        => !(left == right);

    #endregion

    #endregion
}
