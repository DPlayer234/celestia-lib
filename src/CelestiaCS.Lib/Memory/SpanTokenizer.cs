using System;
using CelestiaCS.Lib.Dangerous;

namespace CelestiaCS.Lib.Memory;

/// <summary>
/// An enumerator that splits a <see cref="ReadOnlySpan{T}"/> based on some separator value.
/// </summary>
/// <typeparam name="T"> The type of the span items. </typeparam>
public ref struct ReadOnlySpanTokenizer<T>
    where T : IEquatable<T>
{
    private readonly ReadOnlySpan<T> _buffer;
    private readonly T _splitAt;
    private int _index;

    internal ReadOnlySpanTokenizer(ReadOnlySpan<T> buffer, T splitAt)
    {
        _buffer = buffer;
        _splitAt = splitAt;
        _index = -1;
        Current = default;
    }

    /// <summary> Gets the current token. </summary>
    public ReadOnlySpan<T> Current { get; private set; }

    /// <summary> Moves to the next token. </summary>
    public bool MoveNext()
    {
        var buffer = _buffer;
        int start = _index + 1;

        if ((uint)start >= (uint)buffer.Length)
        {
            _index = buffer.Length;
            return false;
        }

        int next = buffer[start..].IndexOf(_splitAt);
        if (next != -1)
        {
            next += start;

            _index = next;
            Current = buffer[start..next];
            return true;
        }

        _index = -2;
        Current = buffer[start..];
        return true;
    }

    public readonly ReadOnlySpanTokenizer<T> GetEnumerator() => this;

    /// <summary> Returns an equivalent tokenizer with an applied filter. </summary>
    /// <typeparam name="TFilter"> The filter to apply. </typeparam>
    public readonly ReadOnlySpanTokenizer<T, TFilter> Where<TFilter>() where TFilter : ITokenFilter => new(in this);
}

/// <summary>
/// An enumerator that splits a <see cref="Span{T}"/> based on some separator value.
/// </summary>
/// <typeparam name="T"> The type of the span items. </typeparam>
public ref struct SpanTokenizer<T>
    where T : IEquatable<T>
{
    private ReadOnlySpanTokenizer<T> _impl;

    internal SpanTokenizer(Span<T> buffer, T splitAt)
    {
        _impl = new(buffer, splitAt);
    }

    /// <inheritdoc cref="ReadOnlySpanTokenizer{T}.Current"/>
    public readonly Span<T> Current => DangerousSpan.AsMutable(_impl.Current);

    /// <inheritdoc cref="ReadOnlySpanTokenizer{T}.MoveNext"/>
    public bool MoveNext() => _impl.MoveNext();

    public readonly SpanTokenizer<T> GetEnumerator() => this;

    public readonly SpanTokenizer<T, TFilter> Where<TFilter>() where TFilter : ITokenFilter => new(in _impl);
}

/// <summary>
/// An enumerator that splits a <see cref="ReadOnlySpan{T}"/> based on some separator value with a <typeparamref name="TFilter"/>.
/// </summary>
/// <typeparam name="T"> The type of the span items. </typeparam>
public ref struct ReadOnlySpanTokenizer<T, TFilter>
    where T : IEquatable<T>
    where TFilter : ITokenFilter
{
    private ReadOnlySpanTokenizer<T> _impl;

    internal ReadOnlySpanTokenizer(scoped in ReadOnlySpanTokenizer<T> impl)
    {
        _impl = impl;
    }

    /// <summary> Gets the current token. </summary>
    public ReadOnlySpan<T> Current { get; private set; }

    /// <summary> Moves to the next token matching a filter. </summary>
    public bool MoveNext()
    {
        while (_impl.MoveNext())
        {
            if (TFilter.IsAllowed(_impl.Current))
                return true;
        }

        return false;
    }

    public readonly ReadOnlySpanTokenizer<T, TFilter> GetEnumerator() => this;
}

/// <summary>
/// An enumerator that splits a <see cref="Span{T}"/> based on some separator value with a <typeparamref name="TFilter"/>.
/// </summary>
/// <typeparam name="T"> The type of the span items. </typeparam>
public ref struct SpanTokenizer<T, TFilter>
    where T : IEquatable<T>
    where TFilter : ITokenFilter
{
    private ReadOnlySpanTokenizer<T> _impl;

    /// <summary>
    /// SAFTEY: The impl has to be constructed from a mutable span.
    /// </summary>
    internal SpanTokenizer(scoped in ReadOnlySpanTokenizer<T> impl)
    {
        _impl = impl;
    }

    /// <inheritdoc cref="ReadOnlySpanTokenizer{T, TFilter}.Current"/>
    public readonly Span<T> Current => DangerousSpan.AsMutable(_impl.Current);

    /// <inheritdoc cref="ReadOnlySpanTokenizer{T, TFilter}.MoveNext"/>
    public bool MoveNext()
    {
        while (_impl.MoveNext())
        {
            if (TFilter.IsAllowed(_impl.Current))
                return true;
        }

        return false;
    }

    public readonly SpanTokenizer<T, TFilter> GetEnumerator() => this;
}

/// <summary>
/// Filters a span-based token.
/// </summary>
public interface ITokenFilter
{
    /// <summary> Whether the token is allowed. </summary>
    /// <typeparam name="T"> The type of token elements. </typeparam>
    /// <param name="token"> The token to check. </param>
    static abstract bool IsAllowed<T>(ReadOnlySpan<T> token);

    /// <summary>
    /// Filters non-empty tokens.
    /// </summary>
    public readonly struct NotEmpty : ITokenFilter
    {
        /// <inheritdoc/>
        public static bool IsAllowed<T>(ReadOnlySpan<T> token) => !token.IsEmpty;
    }
}

/// <summary>
/// Static class to allow tokenizing <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/> instances.
/// </summary>
public static class SpanTokenizer
{
    /// <summary>
    /// Tokenizes a <see cref="ReadOnlySpan{T}"/> by splitting it at every <paramref name="splitAt"/>.
    /// </summary>
    /// <typeparam name="T"> The type of span items. </typeparam>
    /// <param name="buffer"> The buffer to tokenize. </param>
    /// <param name="splitAt"> The value to split the buffer at. </param>
    /// <returns> The tokenizer enumerator. </returns>
    public static ReadOnlySpanTokenizer<T> Tokenize<T>(this ReadOnlySpan<T> buffer, T splitAt)
        where T : IEquatable<T>
    {
        return new(buffer, splitAt);
    }

    /// <summary>
    /// Tokenizes a <see cref="Span{T}"/> by splitting it at every <paramref name="splitAt"/>.
    /// </summary>
    /// <typeparam name="T"> The type of span items. </typeparam>
    /// <param name="buffer"> The buffer to tokenize. </param>
    /// <param name="splitAt"> The value to split the buffer at. </param>
    /// <returns> The tokenizer enumerator. </returns>
    public static SpanTokenizer<T> Tokenize<T>(this Span<T> buffer, T splitAt)
        where T : IEquatable<T>
    {
        return new(buffer, splitAt);
    }
}
