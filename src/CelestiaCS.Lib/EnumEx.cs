using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CelestiaCS.Lib;

/// <summary>
/// Provides some helper methods to work with enums.
/// </summary>
public static class EnumEx
{
    /// <summary>
    /// Tries to parse the enum by name only and <i>ignoring case</i>.
    /// </summary>
    /// <typeparam name="TEnum"> The type of the enum. </typeparam>
    /// <param name="value"> The value to parse. </param>
    /// <param name="result"> The parsed value. </param>
    /// <returns> If the <paramref name="value"/> was successfully parsed. </returns>
    public static bool TryParseByName<TEnum>(ReadOnlySpan<char> value, out TEnum result)
        where TEnum : struct, Enum
    {
        return TryParseByName(value, StringComparison.OrdinalIgnoreCase, out result);
    }

    /// <summary>
    /// Tries to parse the enum by name only, matching the name with the given <paramref name="comparison"/>.
    /// </summary>
    /// <typeparam name="TEnum"> The type of the enum. </typeparam>
    /// <param name="value"> The value to parse. </param>
    /// <param name="comparison"> The comparison mode to use. </param>
    /// <param name="result"> The parsed value. </param>
    /// <returns> If the <paramref name="value"/> was successfully parsed. </returns>
    public static bool TryParseByName<TEnum>(ReadOnlySpan<char> value, StringComparison comparison, out TEnum result)
        where TEnum : struct, Enum
    {
        value = value.Trim();
        if (value.Length != 0)
        {
            var cache = Info<TEnum>.Instance;
            var names = cache.Names;
            for (int i = 0; i < names.Length; i++)
            {
                if (value.Equals(names[i], comparison))
                {
                    result = cache.Values[i];
                    return true;
                }
            }
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Converts the value to its string representation.
    /// </summary>
    /// <remarks>
    /// This method has identical behavior to <see cref="Enum.ToString"/> but will try to avoid boxing if possible.
    /// </remarks>
    /// <typeparam name="TEnum"> The type of the enum. </typeparam>
    /// <param name="value"> The enum value. </param>
    /// <returns> The string representation of the enum value. </returns>
    public static string ToStringFast<TEnum>(this TEnum value)
        where TEnum : struct, Enum
    {
        return Enum.GetName(value) ?? ToStringSlow(value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static string ToStringSlow(TEnum value)
        {
            return value.ToString(Info<TEnum>.Instance.FallbackFormat);
        }
    }

    /// <summary>
    /// Gets an array of all <typeparamref name="TEnum"/> values without creating a new array per call.
    /// </summary>
    /// <typeparam name="TEnum"> The type of the enum. </typeparam>
    /// <returns> An immutable array of all the enum values. </returns>
    public static ImmutableArray<TEnum> GetValues<TEnum>() where TEnum : struct, Enum
    {
        return Info<TEnum>.Instance.Values;
    }

    private sealed class Info<TEnum>
        where TEnum : struct, Enum
    {
        private Info()
        {
            Names = ImmutableCollectionsMarshal.AsImmutableArray(Enum.GetNames<TEnum>());
            Values = ImmutableCollectionsMarshal.AsImmutableArray(Enum.GetValues<TEnum>());
            Debug.Assert(Names.Length == Values.Length);

            Type tEnum = typeof(TEnum);
            IsFlags = tEnum.IsDefined(typeof(FlagsAttribute), inherit: false);

            // Format string used when a name is not found.
            // Therefore, the by-name route was already checked and we can "skip" it.
            // > D formats as an integer
            // > F does flags formatting
            FallbackFormat = IsFlags ? "F" : "D";
        }

        public static Info<TEnum> Instance { get; } = new();

        public ImmutableArray<string> Names { get; }
        public ImmutableArray<TEnum> Values { get; }
        public bool IsFlags { get; }
        public string FallbackFormat { get; }
    }
}
