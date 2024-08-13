using System;
using System.Text.RegularExpressions;

namespace CelestiaCS.Analyzers.Format;

public static class Naming
{
    public static string CamelToPascal(string input)
    {
        if (input.Length == 0) return string.Empty;

        Span<char> result = input.Length < 128 ? stackalloc char[input.Length] : new char[input.Length];
        input.AsSpan().CopyTo(result);

        if (result[0] == '_')
            result = result[1..];

        if (result.Length == 0)
            return string.Empty;

        result[0] = char.ToUpperInvariant(result[0]);
        return result.ToString();
    }

    public static string CamelToSnakeCase(string name)
    {
        return Regex.Replace(name, @"[A-Z]", m => m.Index == 0 ? m.Value.ToLowerInvariant() : $"_{m.Value.ToLowerInvariant()}");
    }

    public static string EscapeString(string str)
    {
        return str.Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\'", "\\\'")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }
}
