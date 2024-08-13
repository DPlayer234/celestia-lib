using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace CelestiaCS.Lib.State;

/// <summary>
/// Represents that a value may not be supplied.
/// This may be logically different from <see langword="null"/>.
/// </summary>
/// <typeparam name="T"> The type of the value. </typeparam>
[StructLayout(LayoutKind.Auto)]
[JsonConverter(typeof(MaybeJsonConverterFactory))]
[DebuggerDisplay("{DebugValue}")]
public readonly struct Maybe<T> : IEquatable<Maybe<T>>, ICastFrom<Maybe<T>, T>
{
    /// <summary>
    /// An instance without a value. Identical to <see langword="default"/>.
    /// </summary>
    public static Maybe<T> None => default;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly bool _hasValue;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly T? _value;

    /// <summary>
    /// Creates a new instance with a value.
    /// </summary>
    /// <param name="value"> The value contained. </param>
    public Maybe(T value)
    {
        _hasValue = true;
        _value = value;
    }

    /// <summary>
    /// Whether this instance holds a value.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool HasValue => _hasValue;

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    private object? DebugValue => _hasValue ? _value : NoneDebuggerValue.instance;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is Maybe<T> other && Equals(other);
    }

    /// <inheritdoc/>
    public bool Equals(Maybe<T> other)
    {
        return _hasValue
            ? (other._hasValue && EqualityComparer<T>.Default.Equals(_value, other._value))
            : !other._hasValue;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return _hasValue ? (_value?.GetHashCode() ?? 0) : 0;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return _hasValue ? (_value?.ToString() ?? string.Empty) : "<None>";
    }

    /// <summary>
    /// Tries to get the value out.
    /// </summary>
    /// <param name="value"> The value, if there is one. </param>
    /// <returns> Whether there is a value. </returns>
    public bool TryGet([MaybeNullWhen(false)] out T value)
    {
        value = _value;
        return _hasValue;
    }

    /// <summary>
    /// Gets the value from this instance if one is held, otherwise returns the default value for <typeparamref name="T"/>.
    /// </summary>
    public T? GetValueOrDefault() => _value;

    /// <summary>
    /// Gets the value from this instance if one is held, otherwise returns the provided default value.
    /// </summary>
    /// <param name="defaultValue"> The value to return if this instance holds no value. </param>
    public T GetValueOrDefault(T defaultValue) => _hasValue ? _value! : defaultValue;

    /// <summary>
    /// Creates a new instance with the value.
    /// </summary>
    /// <param name="value"> The value. </param>
    public static implicit operator Maybe<T>(T value)
        => new Maybe<T>(value);

    /// <summary>
    /// Extracts the value from this instance.
    /// </summary>
    /// <param name="value"> The value. </param>
    /// <exception cref="InvalidCastException"> The instance holds no value. </exception>
    public static explicit operator T(Maybe<T> value)
        => value._hasValue ? value._value! : throw new InvalidCastException("The Maybe holds no value.");

    public static bool operator ==(Maybe<T> left, Maybe<T> right)
        => left.Equals(right);

    public static bool operator !=(Maybe<T> left, Maybe<T> right)
        => !left.Equals(right);

    static Maybe<T> ICastFrom<Maybe<T>, T>.From(T other) => new(other);
}

/// <summary>
/// Provides additional methods for <see cref="Maybe{T}"/> instances.
/// </summary>
public static class MaybeExtensions
{
    // These methods are extensions to ensure they are only called on ref-able values.

    /// <summary>
    /// Gets the value stored in <paramref name="maybe"/>, otherwise overwrites it with the result of <paramref name="defaultValueFactory"/>() and returns that.
    /// </summary>
    /// <remarks>
    /// This is effectively equivalent to: <c>maybe ??= defaultValueFactory()</c>
    /// </remarks>
    /// <typeparam name="T"> The type of the value. </typeparam>
    /// <param name="maybe"> The maybe to check or write. </param>
    /// <param name="defaultValueFactory"> The default value factory. </param>
    /// <returns> The value in <paramref name="maybe"/> or the result of <paramref name="defaultValueFactory"/>. </returns>
    public static T GetOrWrite<T>(this ref Maybe<T> maybe, Func<T> defaultValueFactory)
    {
        if (maybe.TryGet(out T? result))
        {
            return result;
        }
        else
        {
            result = defaultValueFactory();
            maybe = result;
            return result;
        }
    }

    /// <summary>
    /// Gets the value stored in <paramref name="maybe"/>, otherwise overwrites it with the result of <paramref name="defaultValueFactory"/>(<paramref name="arg"/>) and returns that.
    /// </summary>
    /// <remarks>
    /// This is effectively equivalent to: <c>maybe ??= defaultValueFactory(arg)</c>
    /// </remarks>
    /// <typeparam name="T"> The type of the value. </typeparam>
    /// <param name="maybe"> The maybe to check or write. </param>
    /// <param name="arg"> The argument to the factory. </param>
    /// <param name="defaultValueFactory"> The default value factory. </param>
    /// <returns> The value in <paramref name="maybe"/> or the result of <paramref name="defaultValueFactory"/>. </returns>
    public static T GetOrWrite<T, TArg>(this ref Maybe<T> maybe, TArg arg, Func<TArg, T> defaultValueFactory)
    {
        if (maybe.TryGet(out T? result))
        {
            return result;
        }
        else
        {
            result = defaultValueFactory(arg);
            maybe = result;
            return result;
        }
    }
}

[DebuggerDisplay("{DebugText,nq}")]
internal sealed class NoneDebuggerValue
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal static readonly NoneDebuggerValue instance = new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal string DebugText => "<None>";
}
