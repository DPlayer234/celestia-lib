using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace CelestiaCS.Lib.Format;

/// <summary>
/// Provides extension methods for strings.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Returns an interpolatable object that escapes markdown in the <paramref name="content"/>.
    /// </summary>
    /// <param name="content"> The string to escape. </param>
    /// <returns> The escaping string. </returns>
    public static MdEscapedString EscapeMarkdown(this string? content)
        => new MdEscapedString(content);

    /// <summary>
    /// Returns an interpolatable object that ensures the <paramref name="content"/> is never longer than <paramref name="maxLength"/>.
    /// If truncation happens, appends <paramref name="truncation"/> at the end.
    /// </summary>
    /// <param name="content"> The original string. </param>
    /// <param name="maxLength"> The maximum length of the resulting string. </param>
    /// <param name="truncation"> The truncation text to append if needed. </param>
    /// <returns> The truncating string. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="content"/> is null. </exception>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="maxLength"/> is negative. </exception>
    /// <exception cref="ArgumentException"> <paramref name="truncation"/> is longer than <paramref name="maxLength"/>. </exception>
    public static TruncatedString Truncate(this string content, int maxLength, string? truncation = FormatUtil.Ellipses)
    {
        return new(content, maxLength, truncation);
    }

    /// <summary>
    /// Trims whitespace from the end of the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder"> The string builder. </param>
    /// <returns> The same string builder after being trimmed. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="builder"/> is null. </exception>
    public static StringBuilder TrimEnd(this StringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        int len = builder.Length;
        for (int i = 0; i < len; i++)
        {
            if (!char.IsWhiteSpace(builder[^(i + 1)]))
            {
                builder.Length -= i;
                break;
            }
        }

        return builder;
    }

    /// <summary>
    /// Truncates the length of the <paramref name="builder"/> to <paramref name="maxLength"/>.
    /// If truncation happens, appends <paramref name="truncation"/> at the end.
    /// </summary>
    /// <param name="builder"> The string builder. </param>
    /// <param name="maxLength"> The maximum length of the resulting string. </param>
    /// <param name="truncation"> The truncation text to append if needed. </param>
    /// <returns> The same string builder. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="builder"/> is null. </exception>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="maxLength"/> is negative. </exception>
    /// <exception cref="ArgumentException"> <paramref name="truncation"/> is longer than <paramref name="maxLength"/>. </exception>
    public static StringBuilder Truncate(this StringBuilder builder, int maxLength, string? truncation = FormatUtil.Ellipses)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

        truncation ??= string.Empty;

        if (truncation.Length > maxLength)
            ThrowHelper.Argument(nameof(truncation), "Truncation must not be longer than maxLength.");

        if (builder.Length > maxLength)
        {
            builder.Length = maxLength - truncation.Length;
            builder.Append(truncation);
        }

        return builder;
    }

    /// <summary>
    /// Converts the value of the <paramref name="builder"/> to a string or returns <paramref name="defaultValue"/> if it is empty.
    /// </summary>
    /// <param name="builder"> The string builder. </param>
    /// <param name="defaultValue"> The value to return if empty. </param>
    /// <returns> The resulting string. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="builder"/> is null. </exception>
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static string? ToStringOrDefault(this StringBuilder builder, string? defaultValue)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Length != 0
            ? builder.ToString()
            : defaultValue;
    }

    /// <summary>
    /// Converts the value of a substring of this instance into a <see cref="string"/>.
    /// </summary>
    /// <param name="builder"> The string builder. </param>
    /// <param name="range"> The range to get. </param>
    /// <returns> The specified substring. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="builder"/> is null. </exception>
    /// <exception cref="ArgumentOutOfRangeException"> The range exceeds the limits on the builder. </exception>
    public static string ToString(this StringBuilder builder, Range range)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var (offset, length) = range.GetOffsetAndLength(builder.Length);
        return builder.ToString(offset, length);
    }
}
