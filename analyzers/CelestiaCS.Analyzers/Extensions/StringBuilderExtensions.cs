using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace CelestiaCS.Analyzers.Extensions;

public static class StringBuilderExtensions
{
    public static unsafe StringBuilder Append(this StringBuilder builder, ReadOnlySpan<char> value)
    {
        fixed (char* ptr = value)
        {
            return builder.Append(ptr, value.Length);
        }
    }

    public static TextReader ToTextReader(this StringBuilder builder) => new StringBuilderReader(builder);

    public static SourceText ToSourceText(this StringBuilder builder) => SourceText.From(builder.ToTextReader(), builder.Length, Encoding.UTF8);
}

internal sealed class StringBuilderReader : TextReader
{
    private StringBuilder? _builder;
    private int _pos;

    public StringBuilderReader(StringBuilder builder)
    {
        if (builder is null)
        {
            ThrowBuilderNull();
        }

        _builder = builder;
    }

    public override int Peek()
    {
        var builder = GetBuilder();

        int index = _pos;
        if ((uint)index < (uint)builder.Length)
            return builder[index];

        return -1;
    }

    public override int Read()
    {
        var builder = GetBuilder();

        int index = _pos;
        if ((uint)index < (uint)builder.Length)
        {
            _pos = index + 1;
            return builder[index];
        }

        return -1;
    }

    public override int Read(char[] buffer, int index, int count)
    {
        var builder = GetBuilder();

        int start = _pos;
        int len = Math.Min(buffer.Length, builder.Length - start);
        if (len < count) count = len;

        builder.CopyTo(start, buffer, index, count);

        _pos = start + count;
        return count;
    }

    public override int ReadBlock(char[] buffer, int index, int count)
    {
        return Read(buffer, index, count);
    }

    public override Task<int> ReadAsync(char[] buffer, int index, int count)
    {
        return Task.FromResult(Read(buffer, index, count));
    }

    public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
    {
        return ReadAsync(buffer, index, count);
    }

    public override string? ReadToEnd()
    {
        var builder = GetBuilder();

        int start = _pos;
        if ((uint)start >= (uint)builder.Length)
            return null;

        int len = _pos = builder.Length;
        return builder.ToString(start, len - start);
    }

    public override Task<string?> ReadToEndAsync()
    {
        return Task.FromResult(ReadToEnd());
    }

    public override string? ReadLine()
    {
        var builder = GetBuilder();

        int start = _pos;
        if ((uint)start >= (uint)builder.Length)
            return null;

        int next = start, end = start;
        while ((uint)next < (uint)builder.Length)
        {
            char ch = builder[next++];
            end = next;

            if (ch == '\r' || ch == '\n')
            {
                if (ch == '\r' && (uint)next < (uint)builder.Length && builder[next] == '\n')
                    next++;

                break;
            }
        }

        if (end == start)
        {
            return null;
        }

        _pos = next;
        return builder.ToString(start, end - start);
    }

    public override Task<string?> ReadLineAsync()
    {
        return Task.FromResult(ReadLine());
    }

    protected override void Dispose(bool disposing)
    {
        _builder = null;
        base.Dispose(disposing);
    }

    private StringBuilder GetBuilder()
    {
        var b = _builder;
        if (b is null) ThrowDisposed();
        return b!;
    }

    private void ThrowBuilderNull()
    {
        throw new ArgumentNullException("builder");
    }

    private void ThrowDisposed()
    {
        throw new ObjectDisposedException(GetType().Name);
    }
}
