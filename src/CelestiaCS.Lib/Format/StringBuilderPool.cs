using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace CelestiaCS.Lib.Format;

/// <summary>
/// StringBuilder pool optimized for often building fairly large strings.
/// </summary>
public static class StringBuilderPool
{
    private const int DefaultCapacity = 256;
    private const int MaxCapacity = 2048;

    /// <summary>
    /// Rents a string builder from the pool.
    /// </summary>
    /// <returns> An empty string builder. </returns>
    public static StringBuilder Rent()
    {
        var inst = TlsPerCoreCache<StringBuilder, Marker>.Rent();
        if (inst != null)
        {
            inst.Clear();
            return inst;
        }

        return new StringBuilder(DefaultCapacity);
    }

    /// <summary>
    /// Returns the builder to the pool and returns the string it held.
    /// </summary>
    /// <param name="builder"> The builder to return. </param>
    /// <returns> The final string. </returns>
    public static string ToStringAndReturn(StringBuilder builder)
    {
        string result = builder.ToString();
        Return(builder);
        return result;
    }

    /// <summary>
    /// Returns the builder to the pool and returns the string it held.
    /// </summary>
    /// <param name="builder"> The builder to return. </param>
    /// <param name="defaultValue"> The value to return if the builder was empty. </param>
    /// <returns> The final string. </returns>
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static string? ToStringAndReturn(StringBuilder builder, string? defaultValue)
    {
        string? result = builder.ToStringOrDefault(defaultValue);
        Return(builder);
        return result;
    }

    /// <summary>
    /// Returns the builder to the pool.
    /// </summary>
    /// <param name="builder"> The builder to return. </param>
    public static void Return(StringBuilder builder)
    {
        if (builder.Capacity <= MaxCapacity)
        {
            TlsPerCoreCache<StringBuilder, Marker>.Return(builder);
        }
    }

    private sealed class Marker { }
}
