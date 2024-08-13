using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CelestiaCS.Lib.State;

using _Interlocked = Interlocked;

// There are a couple of expectations for the integer types used with BitSet and related types:
// * `&`, `|`, and `~` work how they do for the built-in integer types.
// * `T.PopCount(T.AllBitsSet)` returns an integer within Int32 range representative of the bit count.
// * `T.One` is an integer with just the first bit set.
// * `T.One << index` can be used to get an integer with a single bit set at `index`.
// * `~(T.One << index)` is equal to `T.AllBitsSet` with just the bit at `index` unset.
// * When using a `default(BitSet<T>)` value: `default(T)` is also a valid instance.
// These expectations aren't verified.
//
// Questionable support:
// * BigInteger: Has some special-cased logic. May as well use BitArray instead.
// * nint/nuint: Architecture dependent size for a set?

/// <summary>
/// Provides static methods to treat integers as sets of bits.
/// </summary>
public static class BitSet
{
    /// <summary> Determines whether the bit at <paramref name="index"/> is 1. </summary>
    /// <typeparam name="T"> The storage type. </typeparam>
    /// <param name="value"> The value to check. </param>
    /// <param name="index"> The 0-based index into the bitfield. </param>
    /// <returns> Whether the bit is 1. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasBit<T>(T value, int index) where T : IBinaryInteger<T>
        => (value & OneBit<T>(index)) != T.Zero;

    /// <summary> Adds the bit at <paramref name="index"/>, setting it to 1. </summary>
    /// <typeparam name="T"> The storage type. </typeparam>
    /// <param name="value"> The value to modify. </param>
    /// <param name="index"> The 0-based index into the bitfield. </param>
    /// <returns> The new value. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T AddBit<T>(T value, int index) where T : IBinaryInteger<T>
        => value | OneBit<T>(index);

    /// <summary> Removes the bit at <paramref name="index"/>, setting it to 0. </summary>
    /// <typeparam name="T"> The storage type. </typeparam>
    /// <param name="value"> The value to modify. </param>
    /// <param name="index"> The 0-based index into the bitfield. </param>
    /// <returns> The new value. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T RemoveBit<T>(T value, int index) where T : IBinaryInteger<T>
    {
        if (typeof(T) == typeof(BigInteger))
        {
            // `~` for BigInteger (understandably) does not add additional leading bits.
            // Unfortunately, that causes the usual logic to cut those bits away.
            // Instead, a conditional subtraction can be used for the same effect.
            T bit = OneBit<T>(index);
            return (value & bit) == T.Zero ? value : value - bit;
        }

        return value & ~OneBit<T>(index);
    }

    /// <summary> Sets the bit at <paramref name="index"/> to <paramref name="set"/>. </summary>
    /// <typeparam name="T"> The storage type. </typeparam>
    /// <param name="value"> The value to modify. </param>
    /// <param name="index"> The 0-based index into the bitfield. </param>
    /// <param name="set"> If the bit is set (to 1). </param>
    /// <returns> The new value. </returns>
    public static T SetBit<T>(T value, int index, bool set) where T : IBinaryInteger<T>
        => set ? AddBit(value, index) : RemoveBit(value, index);

    /// <summary> Reports the supported bit count of <typeparamref name="T"/>. </summary>
    /// <typeparam name="T"> The storage type. </typeparam>
    /// <returns> The type's bit count. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BitCount<T>() where T : IBinaryInteger<T>
    {
        if (typeof(T) == typeof(BigInteger))
        {
            // While the logic below works for fixed-size integer types,
            // due to BigInteger.AllBitsSet only reporting 32 set bits, it's special-cased.
            return int.MaxValue;
        }

        // Wildly enough, for the built-in integers, this gets JITed into a constant.
        return int.CreateTruncating(T.PopCount(T.AllBitsSet));
    }

    /// <summary> Gets a value that has just the bit at <paramref name="index"/> set. </summary>
    /// <typeparam name="T"> The storage type. </typeparam>
    /// <param name="index"> The 0-based index into the bitfield. </param>
    /// <returns> The new value. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T OneBit<T>(int index) where T : IBinaryInteger<T>
    {
        Debug.Assert((uint)index < BitCount<T>());
        return T.One << index;
    }
}

/// <summary>
/// A set of bits with <typeparamref name="T"/>-bits of storage.
/// </summary>
/// <typeparam name="T"> The underlying storage type. </typeparam>
/// <param name="Value"> The value to initialize the field with. </param>
public record struct BitSet<T>(T Value) where T : struct, IBinaryInteger<T>
{
    /// <summary> The backing storage value. </summary>
    public T Value = Value;

    /// <summary> Gets the amount of bits set. </summary>
    public readonly int PopCount => int.CreateTruncating(T.PopCount(Value));

    /// <summary> Gets the amount of available bits. </summary>
    public static int BitCount => BitSet.BitCount<T>();

    /// <summary> Determines whether the bit at <paramref name="index"/> is 1. </summary>
    /// <param name="index"> The 0-based index into the bitfield. </param>
    /// <returns> Whether the bit is 1. </returns>
    public readonly bool HasBit(int index) => BitSet.HasBit(Value, index);

    /// <summary> Adds the bit at <paramref name="index"/>, setting it to 1. </summary>
    /// <param name="index"> The 0-based index into the bitfield. </param>
    public void AddBit(int index) => Value = BitSet.AddBit(Value, index);

    /// <summary> Removes the bit at <paramref name="index"/>, setting it to 0. </summary>
    /// <param name="index"> The 0-based index into the bitfield. </param>
    public void RemoveBit(int index) => Value = BitSet.RemoveBit(Value, index);

    /// <summary> Sets the bit at <paramref name="index"/> to <paramref name="set"/>. </summary>
    /// <param name="index"> The 0-based index into the bitfield. </param>
    /// <param name="set"> If the bit is set (to 1). </param>
    public void SetBit(int index, bool set) => Value = BitSet.SetBit(Value, index, set);

    /// <summary> Tries to add the bit at <paramref name="index"/>. </summary>
    /// <param name="index"> The 0-based index into the bitfield. </param>
    /// <returns> Whether the bit has been changed. </returns>
    public bool TryAddBit(int index)
    {
        T oldValue = Value;
        T newValue = Value = BitSet.AddBit(oldValue, index);
        return newValue != oldValue;
    }

    /// <summary> Tries to remove the bit at <paramref name="index"/>. </summary>
    /// <param name="index"> The 0-based index into the bitfield. </param>
    /// <returns> Whether the bit has been changed. </returns>
    public bool TryRemoveBit(int index)
    {
        T oldValue = Value;
        T newValue = Value = BitSet.RemoveBit(oldValue, index);
        return newValue != oldValue;
    }

    /// <summary> Sets the bit at <paramref name="index"/> to <paramref name="set"/>. </summary>
    /// <param name="index"> The 0-based index into the bitfield. </param>
    /// <param name="set"> If the bit is set (to 1). </param>
    /// <returns> Whether the bit has been changed. </returns>
    public bool TrySetBit(int index, bool set) => set ? TryAddBit(index) : TryRemoveBit(index);

    /// <summary>
    /// A set of bits with <typeparamref name="T"/>-bits of storage using interlocked writes.
    /// </summary>
    /// <remarks>
    /// This type is only supported for 32/64-bit built-in integer types.
    /// </remarks>
    /// <param name="Value"> The value to initialize the field with. </param>
    public record struct Interlocked(T Value)
    {
        private T _value = Value;

        /// <summary> Whether <typeparamref name="T"/> is supported for interlocked access. </summary>
        public static bool IsSupported
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => typeof(T) == typeof(int) || typeof(T) == typeof(uint)
                || typeof(T) == typeof(long) || typeof(T) == typeof(ulong)
                || typeof(T) == typeof(nint) || typeof(T) == typeof(nuint);
        }

        /// <inheritdoc cref="BitSet{T}.Value"/>
        public T Value
        {
            readonly get => Volatile_Read(in _value);
            set => Volatile_Write(ref _value, value);
        }

        /// <inheritdoc cref="BitSet{T}.PopCount"/>
        public readonly int PopCount => int.CreateTruncating(T.PopCount(Value));

        /// <inheritdoc cref="BitSet{T}.HasBit(int)"/>
        public readonly bool HasBit(int index)
        {
            return BitSet.HasBit(Value, index);
        }

        /// <inheritdoc cref="BitSet{T}.TryAddBit(int)"/>
        public bool TryAddBit(int index)
        {
            T bit = BitSet.OneBit<T>(index);
            return (Interlocked_Or(ref _value, bit) & bit) == T.Zero;
        }

        /// <inheritdoc cref="BitSet{T}.TryRemoveBit(int)"/>
        public bool TryRemoveBit(int index)
        {
            T bit = BitSet.OneBit<T>(index);
            return (Interlocked_And(ref _value, ~bit) & bit) != T.Zero;
        }

        /// <inheritdoc cref="BitSet{T}.TrySetBit(int, bool)"/>
        public bool TrySetBit(int index, bool set) => set ? TryAddBit(index) : TryRemoveBit(index);

        private static void AssertSupported()
        {
            if (!IsSupported)
            {
                BitCastThrow.NotSupported(typeof(T));
            }
        }

        #region Interlocked read/write helpers

        private static T Volatile_Read(ref readonly T field)
        {
            AssertSupported();
            return Unsafe.SizeOf<T>() switch
            {
                sizeof(int) => Unsafe.BitCast<int, T>(Volatile.Read(ref Unsafe.As<T, int>(ref Unsafe.AsRef(in field)))),
                sizeof(long) => Unsafe.BitCast<long, T>(Volatile.Read(ref Unsafe.As<T, long>(ref Unsafe.AsRef(in field)))),
                _ => (T)BitCastThrow.NotSupportedReturn(typeof(T))
            };
        }

        private static void Volatile_Write(ref T field, T value)
        {
            AssertSupported();
            switch (Unsafe.SizeOf<T>())
            {
                case sizeof(int): Volatile.Write(ref Unsafe.As<T, int>(ref field), Unsafe.BitCast<T, int>(value)); break;
                case sizeof(long): Volatile.Write(ref Unsafe.As<T, long>(ref field), Unsafe.BitCast<T, long>(value)); break;
                default: BitCastThrow.NotSupported(typeof(T)); break;
            }
        }

        private static T Interlocked_And(ref T field, T value)
        {
            AssertSupported();
            return Unsafe.SizeOf<T>() switch
            {
                sizeof(int) => Unsafe.BitCast<int, T>(_Interlocked.And(ref Unsafe.As<T, int>(ref field), Unsafe.BitCast<T, int>(value))),
                sizeof(long) => Unsafe.BitCast<long, T>(_Interlocked.And(ref Unsafe.As<T, long>(ref field), Unsafe.BitCast<T, long>(value))),
                _ => (T)BitCastThrow.NotSupportedReturn(typeof(T))
            };
        }

        private static T Interlocked_Or(ref T field, T value)
        {
            AssertSupported();
            return Unsafe.SizeOf<T>() switch
            {
                sizeof(int) => Unsafe.BitCast<int, T>(_Interlocked.Or(ref Unsafe.As<T, int>(ref field), Unsafe.BitCast<T, int>(value))),
                sizeof(long) => Unsafe.BitCast<long, T>(_Interlocked.Or(ref Unsafe.As<T, long>(ref field), Unsafe.BitCast<T, long>(value))),
                _ => (T)BitCastThrow.NotSupportedReturn(typeof(T))
            };
        }

        #endregion
    }
}

file static class BitCastThrow
{
    [DoesNotReturn]
    public static void NotSupported(Type type)
    {
        NotSupportedReturn(type);
    }

    [DoesNotReturn]
    public static object NotSupportedReturn(Type type)
    {
        throw new NotSupportedException($"{type.FullName} is not supported as storage for Interlocked bit sets.");
    }
}
