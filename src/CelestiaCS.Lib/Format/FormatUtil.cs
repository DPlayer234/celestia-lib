using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using CelestiaCS.Lib.Format.Items;
using CelestiaCS.Lib.Threading;

namespace CelestiaCS.Lib.Format;

/// <summary>
/// Provides some helper methods for formatting values into interpolated strings.
/// </summary>
public static class FormatUtil
{
    public const string Ellipses = "…";

    /// <summary>
    /// Repeats the given <paramref name="value"/> as often as specified by <paramref name="count"/>.
    /// </summary>
    /// <typeparam name="T"> The type of the value. </typeparam>
    /// <param name="value"> The value to write. </param>
    /// <param name="count"> The repeat count. </param>
    /// <returns> An object that can be interpolated. </returns>
    public static RepeatFormatItem<T> Repeat<T>(T? value, int count)
    {
        return new RepeatFormatItem<T>(value, count);
    }

    public static BarFormatItem<T> BarRel<T>(T? fill, T? missing, int width, double progress)
    {
        ushort fillCount = (ushort)(checked((ushort)width) * Math.Clamp(progress, min: 0.0, max: 1.0));
        ushort missingCount = (ushort)(width - fillCount);
        return new BarFormatItem<T>(fill, missing, fillCount, missingCount);
    }

    public static BarFormatItem<T> BarAbs<T>(T? fill, T? missing, int width, int count)
    {
        ushort fillCount = (ushort)Math.Clamp(count, min: 0, max: checked((ushort)width));
        ushort missingCount = (ushort)(width - fillCount);
        return new BarFormatItem<T>(fill, missing, fillCount, missingCount);
    }

    /// <summary>
    /// Dynamically defines a <paramref name="format"/> for a given <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T"> The type of the value. </typeparam>
    /// <param name="value"> The value to format. </param>
    /// <param name="format"> The format string to use. </param>
    /// <returns> An object that can be interpolated to write <paramref name="value"/> with the given <paramref name="format"/>. </returns>
    public static FormatItem<T> With<T>(T value, string? format)
        where T : ISpanFormattable
    {
        return new FormatItem<T>(value, format);
    }

    /// <summary>
    /// Creates a string by appending the format to a <see cref="StringBuilder"/>. This is more optimized for join-format items and the like.
    /// </summary>
    /// <remarks>
    /// The used <see cref="StringBuilder"/> is pooled.
    /// </remarks>
    /// <param name="handler"> The string handler. </param>
    /// <returns> The final string. </returns>
    public static string SB(ref SBInterpolatedStringHandler handler)
    {
        return handler.ToStringAndClear();
    }

    /// <summary>
    /// Creates a string by appending the format to a <see cref="StringBuilder"/>. This is more optimized for join-format items and the like.
    /// Truncates the final result.
    /// </summary>
    /// <remarks>
    /// The used <see cref="StringBuilder"/> is pooled.
    /// </remarks>
    /// <param name="maxLength"> The maximum length of the result. </param>
    /// <param name="truncation"> The truncation to insert if truncated. </param>
    /// <param name="handler"> The string handler. </param>
    /// <returns> The final string. </returns>
    public static string SB(int maxLength, string truncation, ref SBInterpolatedStringHandler handler)
    {
        handler.Truncate(maxLength, truncation);
        return handler.ToStringAndClear();
    }

    /// <summary>
    /// Returns a string representing the given object, using the invariant culture if it is <see cref="IFormattable"/>.
    /// </summary>
    /// <param name="obj"> The object. </param>
    /// <returns> A string representing the object. </returns>
    public static string ToStringInvariant<T>(T? obj)
    {
#pragma warning disable IDE0038
        // Do not use pattern matching for better code-gen
        return (obj is IFormattable ? ((IFormattable)obj).ToString(null, CultureInfo.InvariantCulture) : obj?.ToString()) ?? string.Empty;
#pragma warning restore IDE0038
    }

    /// <summary>
    /// Tries to copy a string to the <paramref name="destination"/> span.
    /// </summary>
    /// <param name="destination"> The destination span. </param>
    /// <param name="value"> The value to write. </param>
    /// <param name="charsWritten"> The amount of characters written. </param>
    /// <returns> Whether writing succeeded. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryWrite(Span<char> destination, string value, out int charsWritten)
    {
        if (value.TryCopyTo(destination))
        {
            charsWritten = value.Length;
            return true;
        }

        charsWritten = 0;
        return false;
    }

    /// <summary> Uses the current UI culture for formatting within this block. </summary>
    /// <remarks> This sets the <see cref="CultureInfo.CurrentCulture"/> to <see cref="CultureInfo.CurrentUICulture"/> for the scope. </remarks>
    /// <returns> An object that can restore the state. </returns>
    public static ExecutionContextEx.RestoreScope UseUIForFormat()
    {
        // Yeah, this is a bit hacky, but FormatString (and some other custom infra)
        // does not support format providers, so we just go with this. It's rare anyways.

        var ctx = ExecutionContextEx.CaptureAndRestore();
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture;
        return ctx;
    }

    internal static void Split<T>(ReadOnlySpan<T> span, int index, out ReadOnlySpan<T> left, out ReadOnlySpan<T> right)
    {
        left = span[..index];
        right = span[(index + 1)..];
    }

    [InterpolatedStringHandler]
    public struct SBInterpolatedStringHandler
    {
        private StringBuilder _builder;
        private StringBuilder.AppendInterpolatedStringHandler _inter;

        public SBInterpolatedStringHandler(int literalLength, int formattedCount)
        {
            _builder = StringBuilderPool.Rent();
            _inter = new(literalLength, formattedCount, _builder);
        }

        public SBInterpolatedStringHandler(int literalLength, int formattedCount, StringBuilder builder)
        {
            _builder = builder;
            _inter = new(literalLength, formattedCount, builder);
        }

        public readonly void AppendLiteral(string literal) => _builder.Append(literal);

        public readonly void AppendFormatted(string? value) => _builder.Append(value);
        public readonly void AppendFormatted(char value) => _builder.Append(value);
        public readonly void AppendFormatted(ReadOnlySpan<char> value) => _builder.Append(value);

        public void AppendFormatted<T>(T? value)
        {
#pragma warning disable IDE0038
            if (typeof(T).IsValueType && value is IStringBuilderAppendable)
            {
                ((IStringBuilderAppendable)value).AppendTo(_builder);
            }
            else
            {
                _inter.AppendFormatted(value);
            }
#pragma warning restore IDE0038
        }

        public void AppendFormatted<T>(T? value, string format) => _inter.AppendFormatted(value, format);

        internal readonly void Truncate(int maxLength, string truncation) => _builder.Truncate(maxLength, truncation);

        internal string ToStringAndClear()
        {
            StringBuilder builder = _builder;
            this = default;
            return StringBuilderPool.ToStringAndReturn(builder);
        }
    }
}
