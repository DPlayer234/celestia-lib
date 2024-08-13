using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using CelestiaCS.Lib.Format;

namespace CelestiaCS.Lib.Linq;

public static partial class StringCollectionExtensions
{
    /// <summary>
    /// This struct is used by <seealso cref="JoinText{T}(IEnumerable{T}, string)"/>. Its <see langword="default"/> is invalid.
    /// </summary>
    public readonly struct JoinFormatItem<T> : ISpanFormattable, IStringBuilderAppendable
    {
        private readonly IEnumerable<T> _source;
        private readonly string? _joiner;

        internal JoinFormatItem(IEnumerable<T> source, string? joiner)
        {
            ArgumentNullException.ThrowIfNull(source);

            _source = source;
            _joiner = joiner;
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
        /// <param name="format"> An override for the used joiner. </param>
        /// <returns> Whether the operation was successful. </returns>
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default)
        {
            var writer = new SpanFormatWriter(destination, out charsWritten);

            using var enumerator = _source.GetEnumerator();
            if (!enumerator.MoveNext())
                goto Success;

            if (!writer.Append(enumerator.Current))
                goto Fail;

            ReadOnlySpan<char> joiner = format.IsEmpty ? _joiner : format;
            while (enumerator.MoveNext())
            {
                if (!(writer.Append(joiner) && writer.Append(enumerator.Current)))
                    goto Fail;
            }

        Success:
            return true;

        Fail:
            return false;
        }

        public void AppendTo(StringBuilder builder, string? format = null)
        {
            if (typeof(T) == typeof(TempString))
            {
                Debug.Assert(_source is IEnumerable<TempString>);
                var source = Unsafe.As<IEnumerable<TempString>>(_source);

                using var enumerator = source.GetEnumerator();
                if (!enumerator.MoveNext()) return;

                enumerator.Current.AppendTo(builder);

                ReadOnlySpan<char> joiner = string.IsNullOrEmpty(format) ? _joiner : format;
                while (enumerator.MoveNext())
                {
                    builder.Append(joiner);
                    enumerator.Current.AppendTo(builder);
                }
            }
            else
            {
                using var enumerator = _source.GetEnumerator();
                if (!enumerator.MoveNext()) return;

                builder.Append($"{enumerator.Current}");

                ReadOnlySpan<char> joiner = string.IsNullOrEmpty(format) ? _joiner : format;
                while (enumerator.MoveNext())
                {
                    builder.Append(joiner);
                    builder.Append($"{enumerator.Current}");
                }
            }
        }

        string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString();
        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => TryFormat(destination, out charsWritten, format);
    }
}
