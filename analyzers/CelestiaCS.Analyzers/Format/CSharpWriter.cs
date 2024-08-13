using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using CelestiaCS.Analyzers.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace CelestiaCS.Analyzers.Format;

public sealed class CSharpWriter : IDisposable
{
    public const string Eol = "\r\n";

    private static StringBuilder? _cachedSb;

    private readonly StringBuilder _sb;
    private LastLineState _lastLine;
    private int _depth;

    public CSharpWriter()
    {
        _sb = Interlocked.Exchange(ref _cachedSb, null) ?? new();
        _lastLine = LastLineState.Empty;
        _depth = 0;
    }

    public void BeginScope()
    {
        UndoEmptyLine();
        AppendWithDepth("{" + Eol);

        _depth += 1;
        _lastLine = LastLineState.NewScope;
    }

    public void EndScope()
    {
        EndScope(null);
    }

    public void EndScope(string? end)
    {
        UndoEmptyLine();

        _depth -= 1;

        AppendWithDepth("}");
        _sb.Append(end);
        _sb.Append(Eol);
        EmptyLine();
    }

    public void Code(string content)
    {
        AppendMultiline(content);
        AppendEol();
        _lastLine = LastLineState.Empty;
    }

    public void Comment(string content)
    {
        AppendMultiline(content);
        _lastLine = LastLineState.Filled;
    }

    public void Code([InterpolatedStringHandlerArgument("")] ref InterpolatedStringHandler handler)
    {
        handler.AddEolIfNotEmpty();
        AppendEol();
        _lastLine = LastLineState.Empty;
    }

    public void Comment([InterpolatedStringHandlerArgument("")] ref InterpolatedStringHandler handler)
    {
        handler.AddEolIfNotEmpty();
        _lastLine = LastLineState.Filled;
    }

    public void EmptyLine()
    {
        if (_lastLine != LastLineState.Empty)
        {
            AppendEol();
            _lastLine = LastLineState.Empty;
        }
    }

#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
    [Obsolete("Prefer ToSourceText.")]
    public override string ToString()
    {
        return _sb.ToString();
    }
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member

    public SourceText ToSourceText()
    {
        return _sb.ToSourceText();
    }

    public void Dispose()
    {
        _sb.Clear();
        _cachedSb = _sb;
    }

    private void AppendMultiline(string content)
    {
        foreach (ReadOnlySpan<char> line in new SpanLineEnumerator(content.AsSpan()))
        {
            var text = line.TrimEnd();
            if (text.Length != 0)
            {
                AppendWithDepth(text);
            }

            AppendEol();
        }
    }

    private void AppendWithDepth(string line)
    {
        _sb.Append(' ', _depth * 4).Append(line);
    }

    private unsafe void AppendWithDepth(ReadOnlySpan<char> line)
    {
        _sb.Append(' ', _depth * 4).Append(line);
    }

    private void AppendDepth()
    {
        _sb.Append(' ', _depth * 4);
    }

    private void UndoEmptyLine()
    {
        if (_lastLine == LastLineState.Empty)
        {
            _lastLine = LastLineState.Filled;
            _sb.Length -= Eol.Length;
        }
    }

    private void AppendEol()
    {
        _sb.Append(Eol);
    }

    private static bool HasTrailingNewLine(ReadOnlySpan<char> s) => s is [.., '\r' or '\n'];

    private enum LastLineState
    {
        Empty,
        Filled,
        NewScope,
    }

    [InterpolatedStringHandler]
    public struct InterpolatedStringHandler
    {
        private readonly CSharpWriter _writer;
        private bool _hasLineContent;
        private bool _hasContent;

        public InterpolatedStringHandler(int literalLength, int formattedCount, CSharpWriter writer)
        {
            _ = literalLength;
            _ = formattedCount;
            _writer = writer;
            _hasLineContent = false;
            _hasContent = false;
        }
        public void AppendLiteral(string value)
        {
            AppendFormatted(value);
        }

        public void AppendFormatted(ReadOnlySpan<char> value)
        {
            _hasContent = true;

            var e = new SpanLineEnumerator(value);
            if (e.MoveNext())
            {
                while (true)
                {
                    var current = e.Current;
                    if (!current.IsEmpty)
                    {
                        AppendDepthIfEmptyLine();
                        _writer._sb.Append(current);
                    }

                    if (!e.MoveNext()) break;

                    AppendEol();
                }
            }

            if (HasTrailingNewLine(value))
            {
                AppendEol();
            }
        }

        public void AppendFormatted(string? value)
        {
            if (value is null) return;
            AppendFormatted(value.AsSpan());
        }

        public unsafe void AppendFormatted(char value)
        {
            AppendFormatted(new ReadOnlySpan<char>(&value, 1));
        }

        public void AppendFormatted(int value)
        {
            _hasContent = true;

            AppendDepthIfEmptyLine();
            _writer._sb.Append(value);
        }

        //public void AppendFormatted<T>(T value)
        //{
        //    if (value is null) { return; }
        //    AppendLiteral(value.ToString().AsSpan());
        //}

        public readonly void AddEolIfNotEmpty()
        {
            if (_hasContent)
            {
                _writer.AppendEol();
            }
        }

        private void AppendEol()
        {
            _hasLineContent = false;
            _writer.AppendEol();
        }

        private void AppendDepthIfEmptyLine()
        {
            if (!_hasLineContent)
            {
                _hasLineContent = true;
                _writer.AppendDepth();
            }
        }
    }
}

file ref struct SpanLineEnumerator
{
    private ReadOnlySpan<char> _text;

    public SpanLineEnumerator(ReadOnlySpan<char> text)
    {
        _text = text;
        Current = default;
    }

    public ReadOnlySpan<char> Current { get; private set; }

    public bool MoveNext()
    {
        var span = _text;
        if (span.Length == 0) return false;

        int index = span.IndexOfAny('\r', '\n');
        if (index < 0)
        {
            _text = default;
            Current = span;
            return true;
        }

        int nextIndex = index + 1;
        int sepLen = span[index] == '\r' && (uint)nextIndex < (uint)span.Length && span[nextIndex] == '\n' ? 2 : 1;

        _text = span[(index + sepLen)..];
        Current = span[..index];
        return true;
    }

    public readonly SpanLineEnumerator GetEnumerator() => this;
}
