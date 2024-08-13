using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using CelestiaCS.Lib.Memory;

namespace CelestiaCS.Lib.Format;

/// <summary>
/// Holds a temporary string to be interpolated.
/// An instance is only valid until it is interpolated or converted to a string once successfully.
/// </summary>
/// <remarks>
/// Even though this type implements <see cref="IDisposable"/>, it is not necessary to dispose it usually.
/// Manual disposal is supplied for cases where values are not interpolated after creation.
/// </remarks>
[DebuggerDisplay($"{{{nameof(DebuggerDisplay)},nq}}")]
public readonly struct TempString : ISpanFormattable, IStringBuilderAppendable, IDisposable
{
    // To validate safety, a backing class is used.
    // This type is not thread-safe.

    // Note: default represents an empty string

    private readonly Holder? _holder;

    /// <summary>
    /// Initializes a new instance of the <see cref="TempString"/> struct.
    /// </summary>
    /// <remarks>
    /// The <paramref name="handler"/> is invalid after is it passed to this method and cannot be used anymore.
    /// </remarks>
    /// <param name="handler"> The string to later interpolate. </param>
    public TempString(ref InterpolatedStringHandler handler)
    {
        if (handler.builder.Length == 0)
        {
            // For empty builders, just dispose the handler
            _holder = null;
            handler.builder.Dispose();
        }
        else
        {
            // Strictly, default(InterpolatedStringHandler) is valid and would not have an array
            // but once something is appended, it would grab an array.
            Debug.Assert(handler.builder.RentedArray != null, "The handler is never instantiated with a span.");

            // Otherwise, create a holder and reset the handler without disposing it
            _holder = Holder.Create(handler.builder.RentedArray, handler.builder.Length);
            handler.builder = default;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TempString"/> struct by copying a <see cref="string"/>.
    /// </summary>
    /// <param name="source"> The source string. </param>
    public TempString(string? source) : this(source.AsSpan())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TempString"/> struct by copying a <see cref="char"/> span.
    /// </summary>
    /// <param name="source"> The source span. </param>
    public TempString(ReadOnlySpan<char> source)
    {
        if (source.IsEmpty)
        {
            _holder = null;
        }
        else
        {
            char[] buffer = ArrayPool<char>.Shared.Rent(source.Length);
            source.CopyTo(buffer);
            _holder = Holder.Create(buffer, source.Length);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TempString"/> struct.
    /// This invalidates the builder. The builder must be using a rented array.
    /// </summary>
    /// <param name="buffer"> The buffer with the data. This must be sourced from the shared array pool. </param>
    /// <param name="length"> The length of the string. </param>
    internal TempString(char[] buffer, int length)
    {
        Debug.Assert(buffer != null, "Buffer must not be null.");
        Debug.Assert(length != 0, "The caller must handle empty builders.");
        _holder = Holder.Create(buffer, length);
    }

    private TempString(Holder source)
    {
        _holder = source;
    }

    /// <summary>
    /// A value that represents an empty string.
    /// </summary>
    public static TempString Empty => default;

    /// <summary> Whether the string is empty. </summary>
    public bool IsEmpty => _holder == null;

    /// <summary> Gets the number of characters in this instance. </summary>
    public int Length => _holder?.Length ?? 0;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => _holder?.GetDebuggerDisplay() ?? "t\"\"";

    /// <summary>
    /// Turns this instance into a permanent string and returns the underlying buffer.
    /// </summary>
    /// <returns> A permanent string created from this instance. </returns>
    public override string ToString()
    {
        return _holder?.Format() ?? string.Empty;
    }

    /// <summary>
    /// Tries to copy this string into the destination. On and only on success, returns the underlying buffer.
    /// </summary>
    /// <param name="destination"> The buffer to write to. </param>
    /// <param name="charsWritten"> The amount of characters written. </param>
    /// <returns> Whether this string fit into the destination. </returns>
    public bool TryFormat(Span<char> destination, out int charsWritten)
    {
        var src = _holder;
        if (src == null)
        {
            charsWritten = 0;
            return true;
        }

        return src.TryFormat(destination, out charsWritten);
    }

    /// <summary>
    /// Returns the underlying buffer.
    /// </summary>
    /// <remarks>
    /// This behaves the same as if <see cref="ToString"/> or <see cref="TryFormat(Span{char}, out int)"/> were successfully called
    /// but without any of the actual operation.
    /// </remarks>
    public void Dispose()
    {
        _holder?.Dispose();
    }

    /// <summary>
    /// Returns a new instance that is the same as is this one, except it is truncated to never be longer than <paramref name="maxLength"/>.
    /// </summary>
    /// <remarks>
    /// The original instance will be invalidated.
    /// </remarks>
    /// <param name="maxLength"> The maximum length of the resulting value. </param>
    /// <returns> A new truncated instance. </returns>
    public TempString Truncate(int maxLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

        var src = _holder;
        if (src == null) return this;

        if (src.Length > maxLength)
            src.Length = maxLength;

        return new TempString(src);
    }

    /// <summary>
    /// Returns a new instance that is the same as is this one, except it is truncated to never be longer than <paramref name="maxLength"/>.
    /// If truncation happens, appends <paramref name="truncation"/> at the end.
    /// </summary>
    /// <remarks>
    /// The original instance will be invalidated.
    /// </remarks>
    /// <param name="maxLength"> The maximum length of the resulting value. </param>
    /// <param name="truncation"> The truncation text to append if needed. </param>
    /// <returns> A new truncated instance. </returns>
    public TempString Truncate(int maxLength, string truncation)
    {
        if (truncation.Length > maxLength)
            ThrowHelper.Argument(nameof(truncation), "Truncation must not be longer than maxLength.");

        var src = _holder;
        if (src == null) return this;

        if (src.Length > maxLength)
        {
            truncation.CopyTo(src.Buffer.AsSpan(maxLength - truncation.Length));
            src.Length = maxLength;
        }

        return new TempString(src);
    }

    internal void AppendTo(ref ValueStringBuilder builder)
    {
        var src = _holder;
        if (src == null) return;

        builder.Append(src.AsSpan());
        src.Dispose();
    }

    internal void AppendTo(StringBuilder builder)
    {
        var src = _holder;
        if (src == null) return;

        builder.Append(src.AsSpan());
        src.Dispose();
    }

    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString();
    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => TryFormat(destination, out charsWritten);

    void IStringBuilderAppendable.AppendTo(StringBuilder builder, string? format) => AppendTo(builder);

    /// <summary>
    /// Interpolated string handler used to create <see cref="TempString"/> instances.
    /// May not be used after an instance is built with it.
    /// </summary>
    [InterpolatedStringHandler]
    public ref struct InterpolatedStringHandler
    {
        // This must always be initialized with a rented array
        // In practice, this would only fail otherwise if nothing is ever appended either
        internal ValueStringBuilder builder;

        public InterpolatedStringHandler(int literalLength, int formattedCount)
        {
            int capacity = literalLength + formattedCount * 11;
            builder = new(Math.Max(capacity, 128));
        }

        public void AppendLiteral(string value) => builder.Append(value);
        public void AppendFormatted(string? value) => builder.Append(value);
        public void AppendFormatted(char value) => builder.Append(value);
        public void AppendFormatted<T>(T? value) => builder.Append(value);
        public void AppendFormatted<T>(T? value, string? format) => builder.Append(value, format);
        public void AppendFormatted(ReadOnlySpan<char> value) => builder.Append(value);
        public void AppendFormatted(TempString value) => builder.Append(value);
    }

    // Helper class that makes it easily verifiable if a TempInterpolatedItem is being reused.
    // Pooling is too slow to really make it worth pooling these instances, even if the buffers are pooled.
    [DebuggerDisplay($"{{{nameof(DebuggerDisplay)},nq}}")]
    private sealed class Holder
    {
        public required char[]? Buffer;
        public int Length;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay => Buffer == null ? "t.Holder: Empty" : $"t.Holder: \"{Buffer.AsSpan(0, Length)}\"";

        // Impl for the debugger display
        public string GetDebuggerDisplay()
        {
            char[]? buffer = Buffer;
            if (buffer != null)
                return $"t\"{buffer.AsSpan(0, Length)}\"";
            return "t'Already Used";
        }

        // Impl for ToString
        public string Format()
        {
            char[]? buffer = Buffer;
            ValidateBuffer(buffer);

            int length = Length;

            Debug.Assert(buffer.Length >= length);
            string? result = new string(buffer.AsSpan(0, length));

            Return(buffer);
            return result;
        }

        // Impl for TryFormat
        public bool TryFormat(Span<char> destination, out int charsWritten)
        {
            char[]? buffer = Buffer;
            ValidateBuffer(buffer);

            int length = Length;

            Debug.Assert(buffer.Length >= length);
            if (buffer.AsSpan(0, length).TryCopyTo(destination))
            {
                Return(buffer);
                charsWritten = length;
                return true;
            }

            charsWritten = 0;
            return false;
        }

        public ReadOnlySpan<char> AsSpan()
        {
            char[]? buffer = Buffer;
            ValidateBuffer(buffer);

            return buffer.AsReadOnlySpan(0, Length);
        }

        public char[] TakeBuffer()
        {
            char[]? buffer = Buffer;
            ValidateBuffer(buffer);

            Buffer = null;
            return buffer;
        }

        // For manual disposal
        public void Dispose()
        {
            char[]? buffer = Buffer;
            if (buffer != null)
            {
                Return(buffer);
            }
        }

        // Re-uses or creates an instance
        public static Holder Create(char[] buffer, int length)
        {
            Debug.Assert(length > 0);

            return new Holder
            {
                Buffer = buffer,
                Length = length
            };
        }

        // Returns the buffer and this instance to their respective pools
        private void Return(char[] buffer)
        {
            Debug.Assert(buffer == Buffer);

            Buffer = null;
            ArrayPool<char>.Shared.Return(buffer);
        }

        private void ValidateBuffer([NotNull] char[]? buffer)
        {
            if (buffer == null)
            {
                ThrowHelper.InvalidOperation($"Cannot use an instance of {nameof(TempString)} multiple times.");
            }
        }
    }
}
