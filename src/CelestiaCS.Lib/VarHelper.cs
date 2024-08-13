using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CelestiaCS.Lib;

/// <summary>
/// Helper methods for variables.
/// </summary>
[SkipLocalsInit]
public static class VarHelper
{
    /// <summary>
    /// Performs an implicit cast. The point of this method is specify those explicitly with validation at compile time.
    /// </summary>
    /// <remarks>
    /// F.e. casting to interfaces is always allowed, but this validates that the source type actually implements the interface.
    /// </remarks>
    /// <typeparam name="T"> The type to cast to. </typeparam>
    /// <param name="value"> The value. </param>
    /// <returns> The cast value. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Cast<T>(T value) => value;

    /// <summary>
    /// Similar to <see cref="Cast{T}(T)"/> but only serves as a compile-time assert that a cast is possible.
    /// </summary>
    /// <typeparam name="T"> The type to check for. </typeparam>
    /// <param name="value"> The value. </param>
    [Conditional("_NO_EMIT_")]
    public static void IsStatic<T>(T value) { }

    /// <summary>
    /// Determines whether casting <paramref name="value"/> to <typeparamref name="T"/> will pass without any exceptions.
    /// </summary>
    /// <remarks>
    /// This is different from <c><paramref name="value"/> is <typeparamref name="T"/></c> because it will also succeed
    /// if <paramref name="value"/> is <see langword="null"/> and <typeparamref name="T"/> allows <see langword="null"/> values.
    /// </remarks>
    /// <typeparam name="T"> The type to cast to. </typeparam>
    /// <param name="value"> The value to check. </param>
    /// <returns> Whether the cast is allowed. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCompatible<T>(object? value)
    {
        // Skipping the `default(T) is null` check for reference types bizarrely enough leads to
        // better code-gen for all cases except Nullable<T> where it gets slightly worse.
        // This method generates the same ASM with or without this; it only affects inlining
        // with a cast to T after.

        return (!typeof(T).IsValueType || default(T) is null) && value is null || value is T;
    }
}
