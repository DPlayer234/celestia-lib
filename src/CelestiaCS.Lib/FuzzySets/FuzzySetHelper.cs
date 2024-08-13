using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using CelestiaCS.Lib.Dangerous;
using CelestiaCS.Lib.Memory;

namespace CelestiaCS.Lib.FuzzySets;

/// <summary>
/// Provides some static methods used by the <see cref="FuzzySet{TMeta}"/> implementation.
/// </summary>
[SkipLocalsInit]
internal static class FuzzySetHelper
{
    // This is used for ints and may be used twice on the stack at once
    internal const int MaxStackAllocSize = 64;

    /// <summary>
    /// Truncates a rune and filters it to just letters and digits.
    /// </summary>
    /// <param name="src"> The rune to normalize. </param>
    /// <returns> The truncated, normalized rune. </returns>
    internal static char TruncateRune(Rune src)
    {
        // I am not entirely sure whether every letter & digit fits within a char,
        // but I also don't really care and it's ought to be good enough for this.
        if (Rune.IsLetterOrDigit(src))
        {
            return (char)src.Value;
        }

        return '\0';
    }

    /// <summary>
    /// Normalizes the string in <paramref name="source"/> into the <paramref name="target"/> buffer.
    /// Works best if the text is in normalization form KD.
    /// </summary>
    /// <param name="source"> The value to normalize. </param>
    /// <param name="target"> The buffer to write the normalized form to. </param>
    /// <param name="cultureInfo"> The culture in use by the fuzzy set. </param>
    /// <returns> The amount of written characters. </returns>
    internal static int NormalizeText(ReadOnlySpan<char> source, Span<char> target, CultureInfo cultureInfo)
    {
        // We use target as a dangerous span, so we check beforehand that
        // we will never overrun its end. This is fine since callers are
        // expected to allocate buffers this large.
        if (target.Length < source.Length + 2)
            ThrowTargetTooShort();

        // `t`, `target`, and `lowerSource` overlap in memory.
        DangerousSpan<char> t = new(target);
        Span<char> lowerSource = t[1..^1].AsSpan();

        // This cannot fail: The destination is sized based on the source.
        int len = source.ToLower(lowerSource, cultureInfo);
        Debug.Assert((uint)len <= (uint)source.Length);

        lowerSource = lowerSource[..len];
        t[0] = '-';

        int mi = 1;

        // Runes will always be the same amount or less than the source length.
        // Yes, we read and write from the same memory range here.
        // But the writes happen only to parts we're already past.
        foreach (var rune in lowerSource.EnumerateRunes())
        {
            char res = TruncateRune(rune);
            if (res != 0) t[mi++] = res;
        }

        t[mi] = '-';
        return mi + 1;
    }

    /// <summary>
    /// Performs unicode KD normalization (full compatibility decomposition).
    /// </summary>
    /// <param name="value"> The value. </param>
    /// <returns> The normalized values. </returns>
    internal static string PreNormalize(string value)
    {
        // CMBK: In case there is a way to do this with span in the future,
        // do it with spans. We're already working with spans otherwise.
        return value.Normalize(NormalizationForm.FormKD);
    }

    /// <summary> Rents a buffer, normalizes the value into that buffer, and returns a slice holding that buffer. </summary>
    /// <remarks> The slice needs to be returned with <see cref="Return"/> later. </remarks>
    /// <param name="value"> The value to normalize. </param>
    /// <param name="cultureInfo"> The culture in use by the fuzzy set. </param>
    /// <returns> The normalized form of the value in a rented buffer. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static FuzzySlice RentNormalized(string value, CultureInfo cultureInfo)
    {
        value = PreNormalize(value);
        char[] buffer = ArrayPool<char>.Shared.Rent(value.Length + 2);
        int length = NormalizeText(value, buffer.AsSpan(), cultureInfo);
        return new(buffer, length);
    }

    /// <summary> Returns a normalized value that was rented via <see cref="RentNormalized"/>. </summary>
    /// <param name="value"> The normalized value with a rented buffer. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Return(FuzzySlice value)
    {
        ArrayPool<char>.Shared.Return(value.Buffer);
    }

    /// <summary> Normalizes the value into a stack-allocated buffer. May use a rented array if the space is insufficient. </summary>
    /// <remarks> Make sure to call <see cref="SpanRenter{T}.Return"/> on the return value when done. </remarks>
    /// <param name="value"> The value to normalize. </param>
    /// <param name="cultureInfo"> The culture in use by the fuzzy set. </param>
    /// <returns> The normalized form of the value in a rented buffer. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static SpanRenter<char> RentNormalizedStack(string value, Span<char> stack, CultureInfo cultureInfo)
    {
        value = PreNormalize(value);
        var renter = SpanRenter<char>.UseStackOrRent(value.Length + 2, stack);
        int length = NormalizeText(value, renter.Span, cultureInfo);
        renter.SliceSpan(..length);
        return renter;
    }

    /// <summary> Normalizes a string and copies it to a string. </summary>
    /// <param name="value"> The value to normalize. </param>
    /// <param name="cultureInfo"> The culture in use by the fuzzy set. </param>
    /// <returns> The normalized value. </returns>
    internal static string NormalizedToString(string value, CultureInfo cultureInfo)
    {
        var renter = RentNormalizedStack(value, stackalloc char[64], cultureInfo);
        string result = renter.Span.ToString();
        renter.Return();
        return result;
    }

    /// <summary>
    /// Determines whether two spans are similar enough. If their length is close enough, will use Levenshtein distance to calculate a <paramref name="score"/>.
    /// </summary>
    /// <param name="a"> The first span. </param>
    /// <param name="b"> The second span. </param>
    /// <param name="minScore"> The minimum score to be similar. </param>
    /// <param name="score"> The calculated score, if true. Otherwise garbage data. </param>
    /// <returns> Whether the two spans are similar enough. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // Only 1 caller
    internal static bool AreSimilar(ReadOnlySpan<char> a, ReadOnlySpan<char> b, double minScore, out double score)
    {
        Unsafe.SkipInit(out score);

        // int len = Math.Max(a.Length, b.Length)
        // int dist = Math.Abs(a.Length - b.Length)
        int len, dist;
        if (a.Length > b.Length) { len = a.Length; dist = len - b.Length; }
        else { len = b.Length; dist = len - a.Length; }

        if (dist > (1.0 - minScore) * len)
            return false;

        dist = Levenshtein(a, b);
        score = 1.0 - (double)dist / len;
        return score >= minScore;
    }

    /// <summary>
    /// Determines the Levenshtein distance between two spans.
    /// That is the minimum amount of deletions, insertions, and substitutions needed to change <paramref name="a"/> into <paramref name="b"/>.
    /// </summary>
    /// <param name="a"> The first span. </param>
    /// <param name="b"> The second span. </param>
    /// <returns> The distance between the spans. </returns>
    internal static int Levenshtein(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        if (a.IsEmpty) return b.Length;
        if (b.IsEmpty) return a.Length;

        // Be careful with buffer below as its access is unchecked
        var renter = SpanRenter<int>.UseStackOrRent(b.Length + 1, stackalloc int[MaxStackAllocSize]);
        DangerousSpan<int> buffer = new(renter.Span);

        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = i;
        }

        int prev = 0;
        for (int i = 0; i < a.Length; i++)
        {
            prev = i + 1;

            for (int j = 0; j < b.Length; j++)
            {
                int next = a[i] != b[j]
                    ? MathEx.Min(/*del*/ buffer[j + 1] + 1, /*ins*/ prev + 1, /*sub*/ buffer[j] + 1)
                    : buffer[j];

                buffer[j] = prev;
                prev = next;
            }

            buffer[b.Length] = prev;
        }

        renter.Return();
        return prev;
    }

    [DoesNotReturn]
    private static void ThrowTargetTooShort()
    {
        throw new ArgumentException("The target needs to be at least 2 elements longer than the source.", "target");
    }
}
