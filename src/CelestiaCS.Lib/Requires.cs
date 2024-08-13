using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CelestiaCS.Lib;

/// <summary>
/// Provides a set of static methods for argument validation.
/// </summary>
[StackTraceHidden]
public static class Requires
{
    /// <summary>
    /// Asserts that <paramref name="value"/> is not <see langword="null"/>.
    /// </summary>
    /// <typeparam name="T"> The type of the value. </typeparam>
    /// <param name="value"> The value to check. </param>
    public static void NotNull<T>([NotNull] T value, [CallerArgumentExpression(nameof(value))] string expr = "")
    {
        if (value is null)
            ThrowHelper.ArgumentNull(expr);
    }

    /// <inheritdoc cref="NotNullOrEmpty(ICollection, string)"/>
    public static void NotNullOrEmpty([NotNull] string value, [CallerArgumentExpression(nameof(value))] string expr = "")
    {
        if (value is null)
            ThrowHelper.ArgumentNull(expr);

        if (value.Length == 0)
            ThrowHelper.Argument(expr);
    }

    /// <summary>
    /// Asserts that <paramref name="value"/> is not <see langword="null"/> or empty.
    /// </summary>
    /// <param name="value"> The value to check. </param>
    public static void NotNullOrEmpty([NotNull] ICollection value, [CallerArgumentExpression(nameof(value))] string expr = "")
    {
        if (value is null)
            ThrowHelper.ArgumentNull(expr);

        if (value.Count == 0)
            ThrowHelper.Argument(expr);
    }

    /// <summary>
    /// Asserts that <paramref name="value"/> is not <see langword="default"/>.
    /// </summary>
    /// <typeparam name="T"> The type of the value. </typeparam>
    /// <param name="value"> The value to check. </param>
    public static void NotDefault<T>(T value, [CallerArgumentExpression(nameof(value))] string expr = "")
        where T : struct, IEquatable<T>
    {
        if (value.Equals(default))
            ThrowHelper.ArgumentNull(expr);
    }

    /// <summary>
    /// Asserts that <paramref name="value"/> is not <see langword="default"/> or empty.
    /// </summary>
    /// <typeparam name="T"> The type of the value. </typeparam>
    /// <param name="value"> The value to check. </param>
    public static void NotDefaultOrEmpty<T>(T value, [CallerArgumentExpression(nameof(value))] string expr = "")
        where T : struct, IEquatable<T>, ICollection
    {
        if (value.Equals(default))
            ThrowHelper.ArgumentNull(expr);

        if (value.Count == 0)
            ThrowHelper.Argument(expr);
    }

    /// <summary>
    /// Asserts that <paramref name="value"/> is a defined member of the enum type.
    /// </summary>
    /// <typeparam name="T"> The type of the value. </typeparam>
    /// <param name="value"> The value to check. </param>
    public static void IsDefined<T>(T value, [CallerArgumentExpression(nameof(value))] string expr = "") where T : struct, Enum
    {
        if (!Enum.IsDefined(value))
            ThrowHelper.Argument(expr);
    }

    /// <summary>
    /// Asserts that the <paramref name="condition"/> is <see langword="true"/>.
    /// </summary>
    /// <param name="condition"> The condition to check. </param>
    public static void That(bool condition, [CallerArgumentExpression(nameof(condition))] string expr = "")
    {
        if (!condition)
            ThrowHelper.Argument(expr);
    }

    /// <summary>
    /// Asserts that <paramref name="value"/> is contained within the <paramref name="collection"/>.
    /// </summary>
    /// <typeparam name="T"> The type of the value. </typeparam>
    /// <param name="value"> The value to check. </param>
    /// <param name="collection"> The collection to check against. </param>
    public static void Contains<T>(T value, IEnumerable<T> collection, [CallerArgumentExpression(nameof(value))] string expr = "")
    {
        if (!collection.Contains(value))
            ThrowHelper.Argument(expr);
    }

    /// <inheritdoc cref="Contains{T}(T, IEnumerable{T}, string)"/>
    public static void Contains<T>(T? value, ImmutableArray<T> collection, [CallerArgumentExpression(nameof(value))] string expr = "")
    {
        if (!collection.Contains(value!))
            ThrowHelper.Argument(expr);
    }
}
