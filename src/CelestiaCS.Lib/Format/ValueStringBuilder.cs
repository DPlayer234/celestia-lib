using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CelestiaCS.Lib.Format;

/// <summary>
/// A string builder that allows using stack space for the initial buffer and will otherwise use arrays rented from the shared array pool.
/// </summary>
/// <remarks>
/// Mishandling this type may lead to double returns of rented arrays. Do not copy this struct; only move it or pass it by reference.
/// </remarks>
public ref struct ValueStringBuilder
{
    private Span<char> _buffer;
    private char[]? _rentedArray;
    private int _length;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueStringBuilder"/> struct with a given initial (hopefully stack allocated) buffer.
    /// </summary>
    /// <param name="buffer"> The initial buffer to use. </param>
    public ValueStringBuilder(Span<char> buffer)
    {
        _buffer = buffer;
        _rentedArray = null;
        _length = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueStringBuilder"/> struct with a given capacity.
    /// </summary>
    /// <param name="capacity"> The capacity to use. </param>
    public ValueStringBuilder(int capacity)
    {
        char[] arr = ArrayPool<char>.Shared.Rent(capacity);
        _buffer = _rentedArray = arr;
        _length = 0;
    }

    /// <summary> Gets the written text. </summary>
    public readonly ReadOnlySpan<char> Text => _buffer[.._length];

    /// <summary> The whole currently used buffer. </summary>
    public readonly Span<char> Buffer => _buffer;

    /// <summary> The currently used rented array. </summary>
    internal readonly char[]? RentedArray => _rentedArray;

    /// <summary> The length of the written text so far. </summary>
    public int Length
    {
        readonly get => _length;
        set
        {
            if (value <= _length)
            {
                _length = value;
            }
            else
            {
                Append('\0', value - _length);
            }
        }
    }

    /// <summary>
    /// Creates a string from the contents of this builder and then disposes it.
    /// </summary>
    /// <returns> The content as a string. </returns>
    public string ToStringAndDispose()
    {
        string result = ToString();
        Dispose();
        return result;
    }

    /// <summary>
    /// Creates a string from the contents of this builder without touching the builder.
    /// </summary>
    /// <returns> The content as a string. </returns>
    public override readonly string ToString()
    {
        return new string(Text);
    }

    /// <summary>
    /// Creates a <see cref="TempString"/> from the contents of this builder and then disposes it.
    /// </summary>
    /// <remarks>
    /// Avoid using an instance that was created from a span.
    /// </remarks>
    /// <returns> The content as a string. </returns>
    public TempString ToTempStringAndDispose()
    {
        if (_length == 0)
        {
            Dispose();
            return TempString.Empty;
        }

        if (_rentedArray == null)
        {
            Debug.Fail("Should not use span-based builder for TempString.");
            ReplaceBufferWithRentedArray();
        }

        TempString result = new TempString(_rentedArray, _length);
        this = default;
        return result;
    }

    /// <summary>
    /// Clears this instance without disposing it, allowing its buffer to be reused.
    /// </summary>
    public void Clear()
    {
        _length = 0;
    }

    /// <summary>
    /// Returns the rented resources and clears this instance. Usually, you should use a call to
    /// <see cref="ToStringAndDispose"/> or <see cref="ToTempStringAndDispose"/>.
    /// </summary>
    public void Dispose()
    {
        char[]? rentedArray = _rentedArray;
        this = default;
        if (rentedArray != null)
        {
            ArrayPool<char>.Shared.Return(rentedArray);
        }
    }

    /// <summary>
    /// Appends a string to the buffer.
    /// </summary>
    /// <param name="value"> The string to append. </param>
    public void Append(string? value)
    {
        if (value == null) return;

        int length = _length;
        var buffer = _buffer[length..];

    Retry:
        if (value.TryCopyTo(buffer))
        {
            _length = length + value.Length;
        }
        else
        {
            buffer = ExpandCapacity();
            goto Retry;
        }
    }

    /// <summary>
    /// Appends a character to the buffer.
    /// </summary>
    /// <param name="value"> The character to append. </param>
    public void Append(char value)
    {
        int length = _length;
        var buffer = _buffer[length..];

    Retry:
        if (buffer.Length != 0)
        {
            buffer[0] = value;
            _length = length + 1;
        }
        else
        {
            buffer = ExpandCapacity();
            goto Retry;
        }
    }

    /// <summary>
    /// Appends a character repeatedly to the buffer.
    /// </summary>
    /// <param name="value"> The character to append. </param>
    /// <param name="repeatCount"> The number of times to append the character. </param>
    public void Append(char value, int repeatCount)
    {
        if (repeatCount <= 0) return;

        int length = _length;
        var buffer = _buffer[length..];

        while (buffer.Length < repeatCount)
        {
            buffer = ExpandCapacity();
        }

        buffer[..repeatCount].Fill(value);
        _length = length + repeatCount;
    }

    /// <summary>
    /// Appends a value to the buffer.
    /// </summary>
    /// <remarks>
    /// If the <paramref name="value"/> implements <seealso cref="ISpanFormattable"/>, this method delegates to that implementation.
    /// </remarks>
    /// <typeparam name="T"> The type of the value. </typeparam>
    /// <param name="value"> The value to append. </param>
    public void Append<T>(T? value)
    {
        if (value == null) return;

#pragma warning disable IDE0038
        if (value is ISpanFormattable)
        {
            int length = _length;
            var buffer = _buffer[length..];

        Retry:
            if (((ISpanFormattable)value).TryFormat(buffer, out int charsWritten, default, null))
            {
                _length = length + charsWritten;
            }
            else
            {
                buffer = ExpandCapacity();
                goto Retry;
            }
        }
#pragma warning restore IDE0038
        else
        {
            Append(value.ToString());
        }
    }

    /// <summary>
    /// Appends a value to the buffer with a format.
    /// </summary>
    /// <remarks>
    /// If the <paramref name="value"/> implements <seealso cref="ISpanFormattable"/>, this method delegates to that implementation.
    /// </remarks>
    /// <typeparam name="T"> The type of the value. </typeparam>
    /// <param name="value"> The value to append. </param>
    /// <param name="format"> The format for the formattable. </param>
    public void Append<T>(T? value, string? format)
    {
        // Code is similar to method above, however also needs to check
        // for a IFormattable first so it can use that interface's ToString
        // with the correct format.

#pragma warning disable IDE0038
        if (value is IFormattable)
        {
            if (value is ISpanFormattable)
            {
                int length = _length;
                var buffer = _buffer[length..];

            Retry:
                if (((ISpanFormattable)value).TryFormat(buffer, out int charsWritten, format, null))
                {
                    _length = length + charsWritten;
                }
                else
                {
                    buffer = ExpandCapacity();
                    goto Retry;
                }
            }
            else
            {
                Append(((IFormattable)value).ToString(format, null));
            }
        }
#pragma warning restore IDE0038
        else if (value != null)
        {
            Append(value.ToString());
        }
    }

    /// <summary>
    /// Appends a string to the buffer.
    /// </summary>
    /// <param name="value"> The string to append. </param>
    public void Append(scoped ReadOnlySpan<char> value)
    {
        int length = _length;
        var buffer = _buffer[length..];

    Retry:
        if (value.TryCopyTo(buffer))
        {
            _length = length + value.Length;
        }
        else
        {
            buffer = ExpandCapacity();
            goto Retry;
        }
    }

    /// <summary>
    /// Appends a <see cref="TempString"/> to the buffer and then disposes that instance.
    /// </summary>
    /// <param name="value"> The string to append. </param>
    public void Append(TempString value)
    {
        value.AppendTo(ref this);
    }

    /// <summary>
    /// Writes the <paramref name="value"/> into the buffer starting from <paramref name="index"/>.
    /// If this overruns the <see cref="Length"/> or buffer, they are expanded as it is for an append.
    /// </summary>
    /// <param name="index"> The index to insert the value at. </param>
    /// <param name="value"> The value to write. </param>
    public void WriteAt(int index, scoped ReadOnlySpan<char> value)
    {
    Retry:
        var buffer = _buffer[index..];
        if (value.TryCopyTo(buffer))
        {
            _length = Math.Max(_length, index + value.Length);
        }
        else
        {
            _ = ExpandCapacity();
            goto Retry;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Span<char> ExpandCapacity()
    {
        char[]? oldArray = _rentedArray;
        var oldBuffer = _buffer;
        int length = _length;

        // Get new size for rented buffer, rent it, and copy old contents
        int newSize = Math.Max(oldBuffer.Length * 2, 256);
        char[] newArray = ArrayPool<char>.Shared.Rent(newSize);
        oldBuffer[..length].CopyTo(newArray);

        // Update value builder state
        _rentedArray = newArray;
        _buffer = newArray;

        // Return old array, if any
        if (oldArray != null)
        {
            ArrayPool<char>.Shared.Return(oldArray);
        }

        return newArray.AsSpan(length);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ReplaceBufferWithRentedArray()
    {
        Debug.Assert(_rentedArray == null);

        int length = _length;
        char[] newArray = ArrayPool<char>.Shared.Rent(length);
        _buffer[..length].CopyTo(newArray);

        _rentedArray = newArray;
        _buffer = newArray;
    }
}
