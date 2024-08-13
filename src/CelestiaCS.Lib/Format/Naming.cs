using System;
using System.Runtime.CompilerServices;

namespace CelestiaCS.Lib.Format;

/// <summary>
/// Provides static methods to convert names between different naming conventions.
/// </summary>
[SkipLocalsInit]
public static class Naming
{
    /// <summary>
    /// Converts a name in PascalCase or camelCase to snake_case.
    /// </summary>
    /// <param name="name"> The source name. </param>
    /// <returns> The snake_cased name. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="name"/> is null. </exception>
    public static string CamelToSnakeCase(string name)
        => UpperToLowerWithSeparator(name, '_');

    /// <summary>
    /// Converts a name in PascalCase or camelCase to kebab-case.
    /// </summary>
    /// <param name="name"> The source name. </param>
    /// <returns> The kebab-cased name. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="name"/> is null. </exception>
    public static string CamelToKebabCase(string name)
        => UpperToLowerWithSeparator(name, '-');

    /// <summary>
    /// Converts a name in snake_case to camelCase.
    /// </summary>
    /// <param name="name"> The source name. </param>
    /// <returns> The camelCased name. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="name"/> is null. </exception>
    public static string SnakeToCamelCase(string name)
        => SeparatorToUpper(name, '_');

    /// <summary>
    /// Converts a name in kebab-case to camelCase.
    /// </summary>
    /// <param name="name"> The source name. </param>
    /// <returns> The camelCased name. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="name"/> is null. </exception>
    public static string KebabToCamelCase(string name)
        => SeparatorToUpper(name, '-');

    /// <summary>
    /// Splits the words in camelCase with spaces.
    /// </summary>
    /// <param name="name"> The source name. </param>
    /// <returns> The spaced name. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="name"/> is null. </exception>
    public static string SpaceCamelCase(string name)
        => UpperWithSeparator(name, ' ');

    // camel to snake or kebab
    private static string UpperToLowerWithSeparator(string name, char separator)
    {
        ArgumentNullException.ThrowIfNull(name);

        ReadOnlySpan<char> src = name;
        if (src.IsEmpty) return name;

        // Find uppercase letters (ignore first character)
        int i = 1;
        char first = src[0];
        for (; i < src.Length; i++)
        {
            if (IsUpperAscii(src[i]))
            {
                break;
            }
        }

        // In this case, none were found. May reuse the original string.
        if (i == src.Length && !IsUpperAscii(first)) return name;

        // Otherwise, copy the string so far into the builder
        ValueStringBuilder result = new(stackalloc char[128]);

        // Make sure to lowercase the first character too
        if (IsUpperAscii(first))
        {
            result.Append(ToLowerAscii(first));
            result.Append(src[1..i]);
        }
        else
        {
            result.Append(src[..i]);
        }

        // Append the rest correctly
        for (; i < src.Length; i++)
        {
            char c = src[i];
            if (IsUpperAscii(c))
            {
                result.Append(separator);
                result.Append(ToLowerAscii(c));
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToStringAndDispose();
    }

    // kebab or snake to camel
    private static string SeparatorToUpper(string name, char separator)
    {
        ArgumentNullException.ThrowIfNull(name);

        ReadOnlySpan<char> src = name;
        if ((uint)src.Length <= 1) return name;

        // Find separator
        int i = src.IndexOf(separator);

        // In this case, none were found. May reuse the original string.
        if (i == -1 || i >= src.Length - 1) return name;

        // Otherwise, copy the string so far into the builder and append the rest correctly
        ValueStringBuilder result = new(stackalloc char[128]);

        while (i != -1 && i < src.Length - 1)
        {
            result.Append(src[..i]);
            char next = src[i + 1];
            result.Append(IsLowerAscii(next) ? ToUpperAscii(next) : next);

            src = src[(i + 2)..];
            i = src.IndexOf(separator);
        }

        result.Append(src);

        return result.ToStringAndDispose();
    }

    // spacing pascal case
    private static string UpperWithSeparator(string name, char separator)
    {
        ArgumentNullException.ThrowIfNull(name);

        ReadOnlySpan<char> src = name;
        if ((uint)src.Length <= 1) return name;

        // Find uppercase letters (ignore first character)
        int i = 1;
        char first = src[0];
        for (; i < src.Length; i++)
        {
            if (IsUpperAscii(src[i]))
            {
                break;
            }
        }

        // In this case, none were found. May reuse the original string.
        if (i == src.Length) return name;

        // Otherwise, copy the string so far into the builder
        ValueStringBuilder result = new(stackalloc char[128]);

        result.Append(src[..i]);

        // Append the rest correctly
        for (; i < src.Length; i++)
        {
            char c = src[i];
            if (IsUpperAscii(c))
            {
                result.Append(separator);
            }

            result.Append(c);
        }

        return result.ToStringAndDispose();
    }

    private static bool IsLowerAscii(char c) => c is >= 'a' and <= 'z';
    private static bool IsUpperAscii(char c) => c is >= 'A' and <= 'Z';
    private static char ToLowerAscii(char c) => (char)(byte)(c | 0x20);
    private static char ToUpperAscii(char c) => (char)(byte)(c & ~0x20);
}
