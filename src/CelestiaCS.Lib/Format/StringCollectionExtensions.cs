using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CelestiaCS.Lib.Format;
using CelestiaCS.Lib.Memory;

namespace CelestiaCS.Lib.Linq;

/// <summary>
/// Provides some extensions for collections of strings.
/// </summary>
public static partial class StringCollectionExtensions
{
    /// <summary>
    /// Joins the strings with a joiner.
    /// </summary>
    /// <remarks>
    /// The returned value is lazily evaluated by enumerating the <paramref name="source"/> collection only once required.
    /// </remarks>
    /// <typeparam name="T"> The type of items stored. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <param name="joiner"> The string to include between each item. </param>
    /// <returns> The joined string. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="source"/> is null. </exception>
    public static JoinFormatItem<T> JoinText<T>(this IEnumerable<T> source, string? joiner = null)
    {
        return new JoinFormatItem<T>(source, joiner);
    }

    /// <summary>
    /// Joins strings with new lines as the delimiter.
    /// </summary>
    /// <typeparam name="T"> The type of items stored. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <returns> The joined string. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="source"/> is null. </exception>
    public static JoinFormatItem<T> JoinTextLines<T>(this IEnumerable<T> source)
    {
        return new JoinFormatItem<T>(source, "\n");
    }

    /// <summary>
    /// Joins the strings in a naturally sounding way.
    /// </summary>
    /// <remarks>
    /// The returned value is lazily evaluated by enumerating the <paramref name="source"/> collection only once required.
    /// </remarks>
    /// <typeparam name="T"> The type of items stored. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <param name="strongJoin"> The "strong" joiner. This is the joiner used at the very end. </param>
    /// <param name="weakJoin"> The "weak" joiner. This is used everywhere except at the end. </param>
    /// <returns> The naturally joined string. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="source"/> is null. </exception>
    public static NaturalJoinFormatItem<T> JoinNaturalText<T>(this IEnumerable<T> source, string? strongJoin = ", and ", string? weakJoin = ", ")
    {
        return new NaturalJoinFormatItem<T>(source, strongJoin, weakJoin);
    }

    /// <summary>
    /// Joins the strings and truncates at the beginning so the resulting string is never longer than <paramref name="maxLength"/>.
    /// </summary>
    /// <param name="source"> The source strings to join. </param>
    /// <param name="maxLength"> The maximum length of the result. </param>
    /// <param name="joiner"> The string to include between each item. </param>
    /// <param name="truncation"> The string to insert when truncation the string. </param>
    /// <returns> The joined, truncated result. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="source"/> is null. </exception>
    public static string JoinAndTruncateStart(this IEnumerable<string?> source, int maxLength, string? joiner = null, string? truncation = FormatUtil.Ellipses)
    {
        if (source.TryGetNonEnumeratedCount(out int count) && count <= 0)
            return string.Empty;

        int joinerLen = joiner?.Length ?? 0;
        int totalLength = -joinerLen;

        foreach (var item in source)
            totalLength += item != null ? item.Length + joinerLen : joinerLen;

        if (totalLength == 0)
            return string.Empty;

        var renter = SpanRenter<char>.UseStackOrRent(totalLength, stackalloc char[128]);
        var span = renter.Span;

        bool ok = source.JoinText(joiner).TryFormat(span, out int finalLength);
        Debug.Assert(ok && finalLength == totalLength);

        if (totalLength > maxLength)
        {
            span = span[..totalLength][^maxLength..];
            truncation?.AsSpan(0, Math.Min(truncation.Length, maxLength)).CopyTo(span);
        }

        string result = new(span);
        renter.Dispose();
        return result;
    }
}
