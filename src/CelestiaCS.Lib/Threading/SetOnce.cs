using System;
using System.Runtime.InteropServices;
using System.Threading;
using CelestiaCS.Lib.State;

namespace CelestiaCS.Lib.Threading;

/// <summary>
/// Provides a construct that allows setting a value once safely in a multi-threaded scenario.
/// </summary>
/// <remarks>
/// Resetting this instance by setting it to <see langword="default"/> is allowed, but this must
/// not happen concurrently with trying to write or read the value.
/// </remarks>
/// <typeparam name="T"> The type of the value held. </typeparam>
[StructLayout(LayoutKind.Auto)]
public struct SetOnce<T>
{
    // If HasValue is false, something might be writing to _value,
    // so we can't assume it's fine to just read it in case it could be torn.
    // So if it is false, do not read _value!

    private const int IsUnset = default, IsWriting = 1, IsSet = 2;

    private volatile int _state;
    private T? _value;

    /// <summary> Whether a value is held. </summary>
    public readonly bool HasValue => _state == IsSet;

    /// <summary> Gets the held value. </summary>
    /// <exception cref="InvalidOperationException"> Thrown if no value is held. </exception>
    public readonly T Value => HasValue ? _value! : ThrowNoValue();

    /// <summary>
    /// Gets the held value, or <see langword="default"/> if none is set yet.
    /// </summary>
    /// <returns> The held value or <see langword="default"/>. </returns>
    public readonly T? GetValueOrDefault() => HasValue ? _value : default;

    /// <summary>
    /// Returns a <see cref="Maybe{T}"/> that matches the current state of this instance.
    /// </summary>
    /// <returns> This instance as a <see cref="Maybe{T}"/>. </returns>
    public readonly Maybe<T> AsMaybe() => HasValue ? _value! : Maybe<T>.None;

    /// <summary>
    /// Tries to set the value.
    /// </summary>
    /// <remarks>
    /// Only a return-value of <see langword="true"/> indicates that a value is held after the call.
    /// </remarks>
    /// <param name="value"> The value to set. </param>
    /// <returns> Whether the value was set. </returns>
    public bool TrySet(T value)
    {
        if (Interlocked.CompareExchange(ref _state, IsWriting, IsUnset) == IsUnset)
        {
            _value = value;
            _state = IsSet;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to set the value. If the <see cref="Maybe{T}"/> is empty, it fails.
    /// </summary>
    /// <remarks>
    /// Only a return-value of <see langword="true"/> indicates that a value is held after the call.
    /// </remarks>
    /// <param name="value"> The value to set. </param>
    /// <returns> Whether the value was set. </returns>
    public bool TrySet(Maybe<T> value)
    {
        if (value.HasValue && Interlocked.CompareExchange(ref _state, IsWriting, IsUnset) == IsUnset)
        {
            _value = value.GetValueOrDefault();
            _state = IsSet;
            return true;
        }

        return false;
    }

    private readonly T ThrowNoValue()
    {
        ThrowHelper.InvalidOperation("This SetOnce has no value yet.");
        return default!;
    }
}

/// <summary>
/// Provides a construct that allows setting a value once safely in a multi-threaded scenario.
/// </summary>
/// <remarks>
/// Resetting this instance by setting it to <see langword="default"/> is allowed, but this must
/// not happen concurrently with trying to write or read the value.
/// </remarks>
public struct SetOnce
{
    private const int IsUnset = default, IsSet = 1;

    private volatile int _state;

    /// <summary> Whether a value is held. </summary>
    public readonly bool HasValue => _state == IsSet;

    /// <summary>
    /// Tries to set the value.
    /// </summary>
    /// <returns> Whether the value was set. </returns>
    public bool TrySet()
    {
        return Interlocked.CompareExchange(ref _state, IsSet, IsUnset) == IsUnset;
    }
}
