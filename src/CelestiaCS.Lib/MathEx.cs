using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace CelestiaCS.Lib;

/// <summary>
/// Supplies some additional math helper methods.
/// </summary>
public static class MathEx
{
    /// <summary>
    /// Gets the remainder for (<paramref name="a"/> / <paramref name="b"/>). The sign of the result matches the sign of <paramref name="b"/>.
    /// </summary>
    /// <param name="a"> The dividend. </param>
    /// <param name="b"> The divisor. </param>
    /// <returns> The remainder. </returns>
    public static int Mod(int a, int b)
    {
        return a - b * FloorDiv(a, b);
    }

    /// <summary>
    /// Performs a flooring division with integers.
    /// </summary>
    /// <param name="a"> The dividend. </param>
    /// <param name="b"> The divisor. </param>
    /// <returns> The floored division result. </returns>
    public static int FloorDiv(int a, int b)
    {
        return (int)Math.Floor((double)a / b);
    }

    /// <summary>
    /// Performs a ceiling division with integers.
    /// </summary>
    /// <param name="a"> The dividend. </param>
    /// <param name="b"> The divisor. </param>
    /// <returns> The ceiled division result. </returns>
    public static int CeilingDiv(int a, int b)
    {
        return (int)Math.Ceiling((double)a / b);
    }

    /// <summary>
    /// Maps a <paramref name="value"/> in the from range into the to range.
    /// </summary>
    /// <param name="value"> The value to map. </param>
    /// <param name="fromMin"> The minimum value of the source range. </param>
    /// <param name="fromMax"> The maximum value of the source range. </param>
    /// <param name="toMin"> The minimum value of the target range. </param>
    /// <param name="toMax"> The maximum value of the target range. </param>
    /// <returns> The mapped value. </returns>
    public static int Map(int value, int fromMin, int fromMax, int toMin, int toMax)
    {
        return (int)Map<double>(value, fromMin, fromMax, toMin, toMax);
    }

    /// <inheritdoc cref="Map(int, int, int, int, int)"/>
    /// <typeparam name="T"> The numeric type. Must be a floating point type. </typeparam>
    public static T Map<T>(T value, T fromMin, T fromMax, T toMin, T toMax) where T : IFloatingPointIeee754<T>
    {
        return T.Lerp(toMin, toMax, (value - fromMin) / (fromMax - fromMin));
    }

    /// <summary>
    /// Maps a <paramref name="value"/> in the from range into the to range and clamps it to that range.
    /// </summary>
    /// <param name="value"> The value to map. </param>
    /// <param name="fromMin"> The minimum value of the source range. </param>
    /// <param name="fromMax"> The maximum value of the source range. </param>
    /// <param name="toMin"> The minimum value of the target range. </param>
    /// <param name="toMax"> The maximum value of the target range. </param>
    /// <returns> The mapped value. </returns>
    public static int MapClamp(int value, int fromMin, int fromMax, int toMin, int toMax)
    {
        var (clampMin, clampMax) = toMin <= toMax ? (toMin, toMax) : (toMax, toMin);
        return Math.Clamp(Map(value, fromMin, fromMax, toMin, toMax), clampMin, clampMax);
    }

    /// <inheritdoc cref="MapClamp(int, int, int, int, int)"/>
    /// <typeparam name="T"> The numeric type. Must be a floating point type. </typeparam>
    public static T MapClamp<T>(T value, T fromMin, T fromMax, T toMin, T toMax) where T : IFloatingPointIeee754<T>
    {
        var (clampMin, clampMax) = toMin <= toMax ? (toMin, toMax) : (toMax, toMin);
        return T.Clamp(Map(value, fromMin, fromMax, toMin, toMax), clampMin, clampMax);
    }

    /// <summary>
    /// Adds a positive number <paramref name="add"/> to <paramref name="source"/>.
    /// The result will not exceed <paramref name="limit"/>.
    /// </summary>
    /// <typeparam name="T"> The numeric type. </typeparam>
    /// <param name="source"> The source value. </param>
    /// <param name="add"> The positive value to add. </param>
    /// <param name="limit"> The limit. </param>
    /// <returns> The addition result and the excess. The excess will be zero or greater. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (T Result, T Excess) AddToLimit<T>(T source, T add, T limit) where T : INumber<T>
    {
        Debug.Assert(T.IsPositive(add));

        if (source >= limit)
            return (source, add);

        T result = source + add;
        if (result > limit)
            return (limit, result - limit);

        return (result, T.Zero);
    }

    /// <summary>
    /// Subtracts a positive number <paramref name="subtract"/> to <paramref name="source"/>.
    /// The result will not exceed <paramref name="limit"/>.
    /// </summary>
    /// <typeparam name="T"> The numeric type. </typeparam>
    /// <param name="source"> The source value. </param>
    /// <param name="subtract"> The positive value to subtract. </param>
    /// <param name="limit"> The limit. </param>
    /// <returns> The subtraction result and the excess. The excess will be zero or greater. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (T Result, T Excess) SubtractToLimit<T>(T source, T subtract, T limit) where T : INumber<T>
    {
        Debug.Assert(T.IsPositive(subtract));

        if (source <= limit)
            return (source, subtract);

        T result = source - subtract;
        if (result < limit)
            return (limit, limit - result);

        return (result, T.Zero);
    }

    #region Min/Max

        /// <summary>
        /// Compares several values and returns the greatest.
        /// </summary>
        /// <typeparam name="T"> The type of the numbers </typeparam>
        /// <returns> The greatest provided value. </returns>
    public static T Max<T>(T a, T b) where T : INumber<T>
    {
        return T.Max(a, b);
    }

    /// <inheritdoc cref="Max{T}(T, T)"/>
    public static T Max<T>(T a, T b, T c) where T : INumber<T>
    {
        return T.Max(T.Max(a, b), c);
    }

    /// <inheritdoc cref="Max{T}(T, T)"/>
    public static T Max<T>(T a, T b, T c, T d) where T : INumber<T>
    {
        return T.Max(T.Max(a, b), T.Max(c, d));
    }

    /// <inheritdoc cref="Max{T}(T, T)"/>
    public static T Max<T>(ReadOnlySpan<T> values) where T : INumber<T>
    {
        if (values.IsEmpty) return T.Zero;

        T result = values[0];
        for (int i = 1; i < values.Length; i++)
        {
            result = T.Max(result, values[i]);
        }

        return result;
    }

    /// <summary>
    /// Compares several values and returns the smallest.
    /// </summary>
    /// <typeparam name="T"> The type of the numbers </typeparam>
    /// <returns> The smallest provided value. </returns>
    public static T Min<T>(T a, T b) where T : INumber<T>
    {
        return T.Min(a, b);
    }

    /// <inheritdoc cref="Min{T}(T, T)"/>
    public static T Min<T>(T a, T b, T c) where T : INumber<T>
    {
        return T.Min(T.Min(a, b), c);
    }

    /// <inheritdoc cref="Min{T}(T, T)"/>
    public static T Min<T>(T a, T b, T c, T d) where T : INumber<T>
    {
        return T.Min(T.Min(a, b), T.Min(c, d));
    }

    /// <inheritdoc cref="Min{T}(T, T)"/>
    public static T Min<T>(ReadOnlySpan<T> values) where T : INumber<T>
    {
        if (values.IsEmpty) return T.Zero;

        T result = values[0];
        for (int i = 1; i < values.Length; i++)
        {
            result = T.Min(result, values[i]);
        }

        return result;
    }

    #endregion

    /// <summary>
    /// Adds 2 <typeparamref name="T"/>s but limits the result to the storage limits to avoid overflows.
    /// </summary>
    /// <param name="a"> The first number. </param>
    /// <param name="b"> The second number. </param>
    /// <returns> The result. </returns>
    public static T LimitAdd<T>(T a, T b)
        where T : INumber<T>, IMinMaxValue<T>
    {
        if (T.IsZero(b))
            return a;

        T result = unchecked(a + b);
        if (T.IsPositive(b))
        {
            // Adding, result must be larger than A
            if (result < a)
                return T.MaxValue;
        }
        else
        {
            // Subtracting, result must be smaller than A
            if (result > a)
                return T.MinValue;
        }

        return result;
    }
}
