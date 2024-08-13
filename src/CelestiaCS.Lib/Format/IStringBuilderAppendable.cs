using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace CelestiaCS.Lib.Format;

/// <summary>
/// Indicates a value knows how to append itself to a <see cref="StringBuilder"/>.
/// Used by some of the Celestia formatting extensions.
/// </summary>
public interface IStringBuilderAppendable : ISpanFormattable
{
    void AppendTo(StringBuilder builder, string? format = null);
}

public static class StringBuilderAppendableExtensions
{
    public static StringBuilder AppendEx<T>(this StringBuilder builder, T value) where T : IStringBuilderAppendable
    {
        value.AppendTo(builder);
        return builder;
    }

    public static StringBuilder AppendEx(this StringBuilder builder, [InterpolatedStringHandlerArgument("builder")] ref FormatUtil.SBInterpolatedStringHandler handler)
    {
        _ = handler;
        return builder;
    }

    internal static string ToStringImpl<T>(this T value) where T : IStringBuilderAppendable
    {
        var builder = StringBuilderPool.Rent();
        value.AppendTo(builder);
        return StringBuilderPool.ToStringAndReturn(builder);
    }
}
