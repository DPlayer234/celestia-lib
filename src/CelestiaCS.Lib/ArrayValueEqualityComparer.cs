using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CelestiaCS.Lib.Dangerous;
using CelestiaCS.Lib.Memory;

namespace CelestiaCS.Lib;

/// <summary>
/// Defines an <see cref="IEqualityComparer{T}"/> for arrays and lists that compares them for value-equality.
/// </summary>
/// <typeparam name="T"> The type of the array elements. </typeparam>
public sealed class ArrayValueEqualityComparer<T>
    : IEqualityComparer<T[]>
    , IEqualityComparer<ImmutableArray<T>>
    , IEqualityComparer<IReadOnlyList<T>>
{
    private readonly IEqualityComparer<T>? _valueComparer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayValueEqualityComparer{T}"/> class with the default value comparer.
    /// </summary>
    public ArrayValueEqualityComparer()
    {
        if (!typeof(T).IsValueType)
        {
            _valueComparer = EqualityComparer<T>.Default;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayValueEqualityComparer{T}"/> class with a specified value comparer.
    /// </summary>
    /// <param name="valueComparer"> The comparer for the array values. </param>
    public ArrayValueEqualityComparer(IEqualityComparer<T>? valueComparer)
    {
        if (typeof(T).IsValueType)
        {
            if (valueComparer == EqualityComparer<T>.Default)
            {
                // For value types, we want to ensure the default does get devirtualized,
                // so we make sure that we don't store it if the default is explicitly passed.
                valueComparer = null;
            }
        }
        else
        {
            // We ensure that we always hold a comparer for reference types
            // since EqualityComparer<T>.Default doesn't get devirtualized anyways
            valueComparer ??= EqualityComparer<T>.Default;
        }

        _valueComparer = valueComparer;
    }

    /// <summary>
    /// The equality comparer for the values.
    /// </summary>
    public IEqualityComparer<T> ValueComparer => _valueComparer ?? EqualityComparer<T>.Default;

    /// <summary>
    /// Gets an instance that uses the default equality comparer for the array values.
    /// </summary>
    public static ArrayValueEqualityComparer<T> Default { get; } = new();

    /// <summary>
    /// Whether <typeparamref name="T"/> is a small integer type. Used to enable some optimizations.
    /// </summary>
    internal static bool IsTSmallInt
        => typeof(T) == typeof(bool)
        || typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte)
        || typeof(T) == typeof(short) || typeof(T) == typeof(ushort);

    /// <inheritdoc/>
    public bool Equals(T[]? x, T[]? y)
    {
        if (x == y) return true;
        if (x == null || y == null) return false;
        if (x.Length != y.Length) return false;

        return x.AsReadOnlySpan().SequenceEqual(y.AsReadOnlySpan(), _valueComparer);
    }

    /// <inheritdoc/>
    public int GetHashCode(T[] obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return GetHashCode(obj.AsReadOnlySpan());
    }

    /// <inheritdoc/>
    public bool Equals(ImmutableArray<T> x, ImmutableArray<T> y)
    {
        return Equals(ImmutableCollectionsMarshal.AsArray(x), ImmutableCollectionsMarshal.AsArray(y));
    }

    /// <inheritdoc/>
    public int GetHashCode(ImmutableArray<T> obj)
    {
        int result = 0;
        if (!obj.IsDefault)
        {
            result = GetHashCode(obj.AsSpan());
        }
        return result;
    }

    /// <summary>
    /// Gets a hash code for the specified span by its contents.
    /// </summary>
    /// <param name="obj"> The span. </param>
    /// <returns> A hash code for the span. </returns>
    private int GetHashCode(ReadOnlySpan<T> obj)
    {
        HashCode hashCode = default;
        hashCode.Add(obj.Length);

        var valueComparer = _valueComparer;

        // If we have the default comparer for the element type (as far as this type is concerned),
        // we use the HashCode.Add overloads that don't take a comparer.
        // This should have better performance for any type.
        if (IsDefaultComparer(valueComparer))
        {
            if (IsTSmallInt)
            {
                // Special case this since there is a dedicated
                // method on hash codes for spans of bytes.
                hashCode.AddBytes(DangerousSpan.AsBytes(obj));
            }
            else
            {
                foreach (var item in obj)
                    hashCode.Add(item);
            }
        }
        else
        {
            Debug.Assert(valueComparer != null, "Must have comparer for ref types and checks for null on value types.");

            foreach (var item in obj)
                hashCode.Add(item, valueComparer);
        }

        return hashCode.ToHashCode();
    }

    #region IReadOnlyList<T> based

    /// <inheritdoc/>
    public bool Equals(IReadOnlyList<T>? x, IReadOnlyList<T>? y)
    {
        if (x == y) return true;
        if (x == null || y == null) return false;
        if (x.Count != y.Count) return false;

        var valueComparer = _valueComparer;
        if (typeof(T).IsValueType && valueComparer == null)
        {
            return EqualsDefault(x, y);
        }
        else
        {
            Debug.Assert(valueComparer != null);
            return EqualsWithComparer(x, y, valueComparer);
        }
    }

    /// <inheritdoc/>
    public int GetHashCode(IReadOnlyList<T> obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        // Take a faster path for some recognized
        // types that can be converted to spans.
        if (SpanEx.TryGetSpan(obj, out var span))
        {
            return GetHashCode(span);
        }

        HashCode hashCode = default;
        hashCode.Add(obj.Count);

        var valueComparer = _valueComparer;

        // If we have the default comparer for the element type (as far as this type is concerned),
        // we use the HashCode.Add overloads that don't take a comparer.
        // This should have better performance for any type.
        if (IsDefaultComparer(valueComparer))
        {
            if (IsTSmallInt)
            {
                // Match behavior for span version.
                SmallIntHelpers<T>.AddToHashCode(ref hashCode, obj);
            }
            else
            {
                foreach (var item in obj)
                    hashCode.Add(item);
            }
        }
        else
        {
            Debug.Assert(valueComparer != null, "Must have comparer for ref types and checks for null on value types.");

            foreach (var item in obj)
                hashCode.Add(item, valueComparer);
        }

        return hashCode.ToHashCode();
    }

    private static bool EqualsDefault(IReadOnlyList<T> x, IReadOnlyList<T> y)
    {
        // This method is only called for value-type T
        // since reference-type T don't devirtualize EQ<T>.Default
        Debug.Assert(typeof(T).IsValueType);

        for (int i = 0; i < x.Count; i++)
        {
            if (!EqualityComparer<T>.Default.Equals(x[i], y[i])) return false;
        }

        return true;
    }

    private static bool EqualsWithComparer(IReadOnlyList<T> x, IReadOnlyList<T> y, IEqualityComparer<T> valueComparer)
    {
        for (int i = 0; i < x.Count; i++)
        {
            if (!valueComparer.Equals(x[i], y[i])) return false;
        }

        return true;
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDefaultComparer(IEqualityComparer<T>? comparer)
    {
        // The value comparer is never null for reference type arguments.
        // However, we still want the hash code logic to be split based on whether we use the default.
        // This version leads to the minimal needed code-gen for reference and value types respectively.

        if (typeof(T).IsValueType) return comparer == null;
        return comparer == EqualityComparer<T>.Default;
    }
}

#region Special casing GetHashCode for small ints

file static class SmallIntHelpers<T>
{
    // These methods are reasonably rarely used and only valid for very few T.
    // We keep them here to not instantiate them alongside every used T.

    // Importantly, these are only "valid" for `T : unmanaged` with certain sizes.
    // Other types won't corrupt memory, but they may produce nonsense results.
    // In practice, the largest types we use are only up to 2 bytes.

    // Inline, only one caller
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void AddToHashCode(ref HashCode hashCode, IReadOnlyList<T> obj)
    {
        Debug.Assert(ArrayValueEqualityComparer<T>.IsTSmallInt);
        if (obj.Count == 0) return;

        // Time to abuse implementation details to match "AddBytes"
        // without having the whole data as a span.
        // Making sure the result is the same is part of the tests.

        // Size must be a multiple of `sizeof(int)`.
        // That way, unless we hit the final block, we always add exact items to the internal queue.
        Span<byte> buffer = stackalloc byte[32 * sizeof(int) /* 128 */];

        int len;
        using var e = obj.GetEnumerator();
        do
        {
            len = FillUnmanaged(e, buffer);
            hashCode.AddBytes(buffer[..len]);

        } while (len == buffer.Length);
    }

    // Inline, only one caller (the method above)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FillUnmanaged(IEnumerator<T> enumerator, Span<byte> data)
    {
        // Spans passed to this method must be a multiple of sizeof(T) in length
        Debug.Assert(data.Length % Unsafe.SizeOf<T>() == 0);
        Debug.Assert(ArrayValueEqualityComparer<T>.IsTSmallInt);

        int length = 0;

        while (length < data.Length && enumerator.MoveNext())
        {
            Debug.Assert(length + Unsafe.SizeOf<T>() <= data.Length);

            T item = enumerator.Current;
            Unsafe.WriteUnaligned(ref DangerousSpan.GetReferenceAt(data, length), item);
            length += Unsafe.SizeOf<T>();
        }

        Debug.Assert(length <= data.Length);
        return length;
    }

}

#endregion
