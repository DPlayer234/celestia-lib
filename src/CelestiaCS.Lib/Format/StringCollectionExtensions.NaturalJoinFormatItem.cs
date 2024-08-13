using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using CelestiaCS.Lib.Format;

namespace CelestiaCS.Lib.Linq;

public static partial class StringCollectionExtensions
{
    /// <summary>
    /// This struct is used by <seealso cref="JoinNaturalText{T}(IEnumerable{T}, string, string)"/>. Its <see langword="default"/> is invalid.
    /// </summary>
    public readonly struct NaturalJoinFormatItem<T> : ISpanFormattable, IStringBuilderAppendable
    {
        private readonly IEnumerable<T> _source;
        private readonly string? _strongJoin;
        private readonly string? _weakJoin;

        internal NaturalJoinFormatItem(IEnumerable<T> source, string? strongJoin, string? weakJoin)
        {
            ArgumentNullException.ThrowIfNull(source);

            _source = source;
            _strongJoin = strongJoin;
            _weakJoin = weakJoin;
        }

        /// <summary>
        /// Returns the joined string.
        /// </summary>
        public override string ToString() => this.ToStringImpl();

        /// <summary>
        /// Tries to format this instance into a span.
        /// </summary>
        /// <param name="destination"> The destination span. </param>
        /// <param name="charsWritten"> The amount of characters written. </param>
        /// <param name="format"> The format string. </param>
        /// <returns> Whether the operation was successful. </returns>
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default)
        {
            var writer = new SpanFormatWriter(destination, out charsWritten);

            using var enumerator = _source.GetEnumerator();
            if (!enumerator.MoveNext())
                goto Success;

            int lastIndex = _source.Count() - 1;
            int index = 0;

            if (!writer.Append(enumerator.Current))
                goto Fail;

            GetJoins(format, out var strongJoin, out var weakJoin);

            while (enumerator.MoveNext())
            {
                index += 1;

                var sep = index == lastIndex ? strongJoin : weakJoin;
                if (!writer.Append(sep))
                    goto Fail;

                if (!writer.Append(enumerator.Current))
                    goto Fail;
            }

        Success:
            return true;

        Fail:
            return false;
        }

        public void AppendTo(StringBuilder builder, string? format = default)
        {
            using var enumerator = _source.GetEnumerator();
            if (!enumerator.MoveNext()) return;

            int lastIndex = _source.Count() - 1;
            int index = 0;

            builder.Append($"{enumerator.Current}");

            GetJoins(format, out var strongJoin, out var weakJoin);

            while (enumerator.MoveNext())
            {
                index += 1;

                var sep = index == lastIndex ? strongJoin : weakJoin;
                builder.Append(sep);
                builder.Append($"{enumerator.Current}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetJoins(ReadOnlySpan<char> format, out ReadOnlySpan<char> strongJoin, out ReadOnlySpan<char> weakJoin)
        {
            int semicolon = format.IndexOf(';');
            if (semicolon >= 0)
            {
                FormatUtil.Split(format, semicolon, out strongJoin, out weakJoin);
            }
            else
            {
                weakJoin = _weakJoin;
                strongJoin = _strongJoin;
            }
        }

        string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString();
        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => TryFormat(destination, out charsWritten, format);
    }
}
