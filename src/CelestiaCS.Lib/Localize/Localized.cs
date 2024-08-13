using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using CelestiaCS.Lib.Collections;
using CelestiaCS.Lib.Json;
using CelestiaCS.Lib.Linq;
using CelestiaCS.Lib.State;

namespace CelestiaCS.Lib.Localize;

/// <summary> Represents a localized resource. </summary>
/// <typeparam name="T"> The type of the resource. </typeparam>
[JsonConverter(typeof(JsonLocalizedConverterFactory))]
public sealed class Localized<T>
    where T : notnull
{
    private readonly T _invariant;
    private readonly ImmutableArray<KeyValuePair<string, T>> _data;

    /// <summary> Initializes a new instance of the <see cref="Localized{T}"/> class with only invariant data. </summary>
    /// <param name="invariant"> The invariant data. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="invariant"/> is null. </exception>
    public Localized(T invariant)
    {
        ArgumentNullException.ThrowIfNull(invariant);
        _invariant = invariant;
    }

    /// <summary> Initializes a new instance of the <see cref="Localized{T}"/> class. </summary>
    /// <remarks> The entries must contain one for the invariant/fallback culture. </remarks>
    /// <param name="entries"> The localized entries. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="entries"/> is null. </exception>
    /// <exception cref="ArgumentException"> An entry is null or no invariant data is present. </exception>
    public Localized(IEnumerable<KeyValuePair<CultureInfo, T>> entries)
    {
        (_invariant, _data) = InitCore<CultureInfo, CultureToKey>(entries);
    }

    internal Localized(T invariant, ImmutableArray<KeyValuePair<string, T>> data)
    {
        _invariant = invariant;
        _data = data;
    }

    /// <summary> Gets the value for the <see cref="CultureInfo.InvariantCulture"/>. </summary>
    public T InvariantValue => _invariant;

    /// <summary> Gets the value for the <see cref="CultureInfo.CurrentUICulture"/>. </summary>
    public T Value => GetValue(CultureInfo.CurrentUICulture);

    /// <summary> Gets all stored localized values. </summary>
    public ImmutableArray<KeyValuePair<string, T>> LocalizedValues => _data.OrEmpty();

    private IEnumerable<string> CultureNames => _data is { IsDefault: false } d ? _data.Select(d => d.Key) : Enumerable.Empty<string>();

    /// <summary>
    /// Gets a value for the specified <paramref name="culture"/>.
    /// </summary>
    /// <param name="culture"> The culture to use. </param>
    /// <returns> The appropriate value. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="culture"/> is null. </exception>
    public T GetValue(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        T? result;
        var data = _data;
        if (!data.IsDefault)
        {
            while (culture != CultureInfo.InvariantCulture)
            {
                string name = culture.Name;
                foreach (var entry in data)
                {
                    if (entry.Key == name)
                    {
                        result = entry.Value;
                        goto Found;
                    }
                }

                culture = culture.Parent;
            }
        }

        result = _invariant;

    Found:
        return result;
    }

    /// <summary> EARGERLY select a localized resource into the present items. </summary>
    /// <remarks> This applies a transform to each locale's resource and returns a new item that holds those transformed results. </remarks>
    /// <typeparam name="TNext"> The type to select. </typeparam>
    /// <param name="selector"> The element selector. </param>
    /// <returns> A new localized resource with the sub-elements. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="selector"/> is null. </exception>
    /// <exception cref="ArgumentException"> A result of <paramref name="selector"/> is null. </exception>
    public Localized<TNext> Select<TNext>(Func<T, TNext> selector)
        where TNext : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);

        TNext invariant = selector(_invariant);
        if (invariant == null)
            Localized.ThrowInvalidSelect();

        if (_data.IsDefault)
            return new Localized<TNext>(invariant);

        TinyDictionary<string, TNext>? data = [];
        foreach (var entry in _data)
        {
            TNext datum = selector(entry.Value);
            if (datum == null)
                Localized.ThrowInvalidSelect();

            data.Add(entry.Key, datum);
        }

        return new Localized<TNext>(invariant, Localizer.ToDataField(data));
    }

    /// <summary> EARGERLY select a localized resource into the present items. </summary>
    /// <remarks> This applies a transform to each locale's resource and returns a new item that holds those transformed results. </remarks>
    /// <typeparam name="TNext"> The type to select. </typeparam>
    /// <param name="selector"> The element selector. </param>
    /// <returns> A new localized resource with the sub-elements. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="selector"/> is null. </exception>
    /// <exception cref="ArgumentException"> A result of <paramref name="selector"/> is null. </exception>
    public Localized<TNext> Select<TNext>(Func<T, CultureInfo, TNext> selector)
        where TNext : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);

        TNext invariant = selector(_invariant, CultureInfo.InvariantCulture);
        if (invariant == null)
            Localized.ThrowInvalidSelect();

        if (_data.IsDefault)
            return new Localized<TNext>(invariant);

        TinyDictionary<string, TNext>? data = [];
        foreach (var entry in _data)
        {
            TNext datum = selector(entry.Value, CultureInfo.GetCultureInfo(entry.Key));
            if (datum == null)
                Localized.ThrowInvalidSelect();

            data.Add(entry.Key, datum);
        }

        return new Localized<TNext>(invariant, Localizer.ToDataField(data));
    }

    /// <summary> Returns a new instance that has all data trimmed from it that is equal to the corresponding fallback data. </summary>
    /// <param name="comparer"> The equality comparer to use. </param>
    /// <returns> A trimmed localized. </returns>
    public Localized<T> Trim(IEqualityComparer<T>? comparer = null)
    {
        if (_data.IsDefault) return this;

        comparer ??= EqualityComparer<T>.Default;

        TinyDictionary<string, T> data = [];
        foreach (var entry in _data)
        {
            var culture = CultureInfo.GetCultureInfo(entry.Key);
            var fallback = GetValue(culture.Parent);

            if (!comparer.Equals(fallback, entry.Value))
            {
                data.Add(entry.Key, entry.Value);
            }
        }

        return data.Count == _data.Length ? this : new Localized<T>(_invariant, Localizer.ToDataField(data));
    }

    /// <summary> Zips two localized resources together, selecting them into a new resource. </summary>
    /// <typeparam name="TOther"> The type of the other instance. </typeparam>
    /// <typeparam name="TNext"> The type of the result values. </typeparam>
    /// <param name="other"> The other localized resource. </param>
    /// <param name="selector"> The result selector. </param>
    /// <returns> The final new resource. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="other"/> or <paramref name="selector"/> is null. </exception>
    /// <exception cref="ArgumentException"> A result of <paramref name="selector"/> is null. </exception>
    public Localized<TNext> Zip<TOther, TNext>(Localized<TOther> other, Func<T, TOther, TNext> selector)
        where TOther : notnull
        where TNext : notnull
    {
        ArgumentNullException.ThrowIfNull(other);
        ArgumentNullException.ThrowIfNull(selector);

        TNext invariant = selector(_invariant, other._invariant);
        if (invariant == null)
            Localized.ThrowInvalidSelect();

        var cultures = CultureNames.Concat(other.CultureNames).Distinct();

        TinyDictionary<string, TNext> data = [];
        foreach (var key in cultures)
        {
            var culture = CultureInfo.GetCultureInfo(key);
            TNext datum = selector(GetValue(culture), other.GetValue(culture));
            if (datum == null)
                Localized.ThrowInvalidSelect();

            data.Add(key, datum);
        }

        return new Localized<TNext>(invariant, Localizer.ToDataField(data));
    }

    /// <summary>
    /// Converts a localized value of a more derived type to some base type.
    /// </summary>
    /// <typeparam name="TDerived"> The derived type. </typeparam>
    /// <param name="source"> The source localized value. </param>
    /// <returns> The less derived localized value. </returns>
    public static Localized<T> UpCast<TDerived>(Localized<TDerived> source) where TDerived : T
    {
        return source.Select<T>(r => r);
    }

    /// <summary> Enumerates all stored resource pairs. </summary>
    /// <returns> A collection of all resource pairs. </returns>
    public IEnumerable<KeyValuePair<string, T>> Enumerate()
    {
        var invariantPair = KeyValuePair.Create(string.Empty, _invariant);
        var data = ImmutableCollectionsMarshal.AsArray(_data);
        return data is null ? EnumerableEx.Once(invariantPair) : data.Prepend(invariantPair);
    }

    /// <summary> Returns a string represents the value for the current UI culture. </summary>
    /// <returns> A string that represents the object. </returns>
    public override string ToString() => Value?.ToString() ?? string.Empty;

    /// <summary> Zips together several resources of another type into a new resource. </summary>
    /// <typeparam name="TOther"> The other type. </typeparam>
    /// <param name="source"> The source localized resources. </param>
    /// <param name="selector"> The result selector. </param>
    /// <returns> The final, zipped resource. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="source"/> or <paramref name="selector"/> is null. </exception>
    /// <exception cref="ArgumentException"> A result of <paramref name="selector"/> is null. </exception>
    public static Localized<T> Zip<TOther>(IEnumerable<Localized<TOther>> source, Func<IEnumerable<TOther>, T> selector) where TOther : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var local = source.ToArray();

        T invariant = selector(local.Select(r => r._invariant));
        if (invariant == null)
            Localized.ThrowInvalidSelect();

        var cultures = local.SelectMany(r => r.CultureNames).Distinct();
        TinyDictionary<string, T> data = [];
        foreach (var key in cultures)
        {
            var culture = CultureInfo.GetCultureInfo(key);
            T datum = selector(local.Select(r => r.GetValue(culture)));
            if (datum == null)
                Localized.ThrowInvalidSelect();

            data.Add(key, datum);
        }

        return new Localized<T>(invariant, Localizer.ToDataField(data));
    }

    /// <summary> Creates a new <see cref="Localized{T}"/> with the specified pairs, using the culture names directly, without validating whether such a culture exists. </summary>
    /// <remarks> The entries must contain one for the invariant/fallback culture, represented via an empty string. </remarks>
    /// <param name="entries"> The localized entries. </param>
    /// <returns> A new instance of the <see cref="Localized{T}"/> class. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="entries"/> is null. </exception>
    /// <exception cref="ArgumentException"> An entry is null or no invariant data is present. </exception>
    public static Localized<T> CreateUnchecked(IEnumerable<KeyValuePair<string, T>> entries)
    {
        var (i, d) = InitCore<string, StringToKey>(entries);
        return new Localized<T>(i, d);
    }

    /// <summary> Creates a <see cref="Localized{T}"/>, directly wrapping the provided parameters. </summary>
    /// <param name="invariant"> The invariant value. </param>
    /// <param name="entries"> The localized data. </param>
    /// <returns> A new instance of the <see cref="Localized{T}"/> class. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="invariant"/> is null. </exception>
    /// <exception cref="ArgumentException"> An entry is null. </exception>
    public static Localized<T> CreateUnsafe(T invariant, ImmutableArray<KeyValuePair<string, T>> entries)
    {
        ArgumentNullException.ThrowIfNull(invariant);

        if (entries.IsDefaultOrEmpty)
            return new Localized<T>(invariant);

        foreach (var entry in entries)
        {
            if (entry.Key is null || entry.Value is null)
                Localized.ThrowInvalidEntry();
        }

        return new Localized<T>(invariant, entries);
    }

    // I'm not particularly happy with an allocating implicit conversion, but I'll just say it's fine for this in particular. At least it's lossless.
    // The inverse conversion would be lossy (and potentially not what the user wanted), so we don't provide that.

    /// <summary> Creates a <see cref="Localized{T}"/> with invariant data. </summary>
    /// <param name="value"> The invariant data. </param>
    public static implicit operator Localized<T>(T value) => new(value);

    private static (T invariant, ImmutableArray<KeyValuePair<string, T>> data) InitCore<TKey, TToKey>(IEnumerable<KeyValuePair<TKey, T>> entries) where TToKey : IToKey<TKey>
    {
        ArgumentNullException.ThrowIfNull(entries);

        Maybe<T> invariant = Maybe<T>.None;
        TinyDictionary<string, T> data = [];
        foreach (var entry in entries)
        {
            if (entry.Key is null || entry.Value is null)
                Localized.ThrowInvalidEntry();

            string key = TToKey.ToKey(entry.Key);
            if (key.Length != 0)
                data.Add(key, entry.Value);
            else if (invariant.HasValue)
                Localized.ThrowDuplicateInvariant();
            else
                invariant = entry.Value;
        }

        if (!invariant.TryGet(out T? rInvariant))
            Localized.ThrowNoInvariant();

        return (rInvariant, Localizer.ToDataField(data));
    }

    private interface IToKey<TIn>
    {
        static abstract string ToKey(TIn value);
    }

    private readonly struct StringToKey : IToKey<string>
    {
        public static string ToKey(string value) => value;
    }

    private readonly struct CultureToKey : IToKey<CultureInfo>
    {
        public static string ToKey(CultureInfo value) => value.Name;
    }
}

file static class Localized
{
    [DoesNotReturn]
    public static void ThrowInvalidEntry()
    {
        ThrowHelper.Argument("entries", "Entries must not be null.");
    }

    [DoesNotReturn]
    public static void ThrowInvalidSelect()
    {
        ThrowHelper.ArgumentCombined("Select results must not be null.");
    }

    [DoesNotReturn]
    public static void ThrowNoInvariant()
    {
        ThrowHelper.Argument("entries", "Entries must contain an entry for '' (empty string) representing the invariant culture/ultimate fallback.");
    }

    [DoesNotReturn]
    public static void ThrowDuplicateInvariant()
    {
        ThrowHelper.Argument("entries", "Entries must not contain multiple invariant entries with key '' (empty string).");
    }
}
