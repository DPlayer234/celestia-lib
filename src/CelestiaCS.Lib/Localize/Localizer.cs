using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using CelestiaCS.Lib.Collections;
using CelestiaCS.Lib.Json;
using CelestiaCS.Lib.State;

namespace CelestiaCS.Lib.Localize;

using StreamResolver = Func<CultureInfo, Stream>;

/// <summary>
/// Represents a localized resource that can be loaded from a JSON file.
/// </summary>
public interface ILocalizedResource
{
    /// <summary> Gets the "key" to load it from. </summary>
    /// <remarks> This will represent a path in the form of "Localize/{PathKey}.{Culture}.json" </remarks>
    static abstract string PathKey { get; }

    /// <summary> Gets serializer options to use for deserializing the resource data. </summary>
    static virtual JsonSerializerOptions JsonSerializerOptions => DefaultJsonSerializerOptions;

    /// <summary> Gets the default serializer options used by <see cref="JsonSerializerOptions"/>. </summary>
    static JsonSerializerOptions DefaultJsonSerializerOptions { get; } = new JsonSerializerOptions
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { JsonTypeInfoModifiers.MakeAllPropertiesRequired }
        }
    };

    internal static StreamResolver GetStreamResolver(string key) => cultureInfo =>
    {
        const string Ext = ".json";
        string filename = cultureInfo.Name.Length == 0 ? $"{key}{Ext}" : $"{key}.{cultureInfo.Name}{Ext}";
        var path = Path.Join(AppHelper.BaseDirectory, "Localize", filename);
        return File.OpenRead(path);
    };
}

/// <summary>
/// Provides centralized access to a JSON based resource.
/// </summary>
/// <typeparam name="T"> The type of the resource. </typeparam>
public static class Localizer<T> where T : ILocalizedResource
{
    private static Localized<T>? _root;

    /// <summary> Gets the root resource. Lazily loaded when first accessed. </summary>
    /// <remarks> <see cref="Localizer.SupportedCultures"/> must be set first. </remarks>
    /// <exception cref="InvalidOperationException"> <see cref="Localizer.SupportedCultures"/> was not set. </exception>
    /// <exception cref="JsonException"> The resource file data could not be loaded. </exception>
    /// <exception cref="IOException"> The resource file could not be found or loaded. </exception>
    public static Localized<T> Root => _root ?? LoadRoot();

    private static Localized<T> LoadRoot()
    {
        return _root = Localizer.LoadLocalized<T>(T.PathKey, T.JsonSerializerOptions);
    }
}

/// <summary>
/// Provides helper methods for use with <see cref="Localized{T}"/> and <see cref="Localizer{T}"/>.
/// </summary>
public static class Localizer
{
    private static ImmutableArray<CultureInfo> _supportedCultures;
    private static readonly CultureCache _requestToSupportedCultureCache = new();

    /// <summary>
    /// Gets or sets the supported cultures. The invariant culture must not be specified.
    /// </summary>
    public static ImmutableArray<CultureInfo> SupportedCultures
    {
        get => _supportedCultures;
        set
        {
            _supportedCultures = value;
            _requestToSupportedCultureCache.Clear();
        }
    }

    /// <summary>
    /// Gets a supported culture by name. If not found, and not a parent of a supported one, returns the invariant culture.
    /// </summary>
    /// <param name="name"> The name of the culture to get. </param>
    /// <returns> The found culture. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="name"/> is null. </exception>
    public static CultureInfo GetSupportedCulture(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (name.Length == 0)
            return CultureInfo.InvariantCulture;

        return _requestToSupportedCultureCache.Read(name);
    }

    /// <summary>
    /// Creates a <see cref="Localized{T}"/> by calling a delegate for every supported culture and the invariant culture.
    /// </summary>
    /// <remarks>
    /// The appropriate culture is set as the <see cref="CultureInfo.CurrentUICulture"/> for the delegate call.
    /// Guarantees that the original value is restored before this method returns.
    /// <see cref="SupportedCultures"/> must be set first.
    /// </remarks>
    /// <typeparam name="T"> The type of resource to create. </typeparam>
    /// <param name="create"> The delegate that creates the resource. </param>
    /// <returns> A localized resource. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="create"/> is null. </exception>
    /// <exception cref="ArgumentException"> <paramref name="create"/> returned null. </exception>
    /// <exception cref="InvalidOperationException"> <see cref="SupportedCultures"/> was not set. </exception>
    public static Localized<T> CreateLocalized<T>(Func<CultureInfo, T> create) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(create);
        var cultures = GetSupportedCulturesOrThrow();

        // Restore this later
        var originalUICulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            var invariant = create(CultureInfo.InvariantCulture);
            if (invariant == null) Invalid();

            TinyDictionary<string, T> data = [];
            foreach (var culture in cultures)
            {
                CultureInfo.CurrentUICulture = culture;
                var res = create(culture);
                if (res == null) Invalid();

                data.Add(culture.Name, res);
            }

            return new Localized<T>(invariant, ToDataField(data));
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalUICulture;
        }

        [DoesNotReturn]
        static void Invalid() => ThrowHelper.ArgumentCombined("Resource must not be null.");
    }

    /// <summary> Loads a localized resource. </summary>
    /// <remarks> <see cref="SupportedCultures"/> must be set first. </remarks>
    /// <param name="pathKey"> The path key to use. </param>
    /// <param name="jsonSerializerOptions"> The JSON serializer options to use. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="pathKey"/> is null. </exception>
    /// <exception cref="InvalidOperationException"> <see cref="SupportedCultures"/> was not set. </exception>
    /// <exception cref="JsonException"> The resource data could not be loaded. </exception>
    public static Localized<T> LoadLocalized<T>(string pathKey, JsonSerializerOptions? jsonSerializerOptions = null) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(pathKey);
        return LoadLocalized<T>(ILocalizedResource.GetStreamResolver(pathKey), jsonSerializerOptions);
    }

    /// <summary> Loads a localized resource. </summary>
    /// <remarks> <see cref="SupportedCultures"/> must be set first. </remarks>
    /// <param name="resolver"> The method that can resolve streams to files of this resource. </param>
    /// <param name="jsonSerializerOptions"> The JSON serializer options to use. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="resolver"/> is null. </exception>
    /// <exception cref="InvalidOperationException"> <see cref="SupportedCultures"/> was not set. </exception>
    /// <exception cref="JsonException"> The resource data could not be loaded. </exception>
    public static Localized<T> LoadLocalized<T>(StreamResolver resolver, JsonSerializerOptions? jsonSerializerOptions = null) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(resolver);

        TinyDictionary<string, LoadEntry> documents = [];
        try
        {
            LoadEntry.LoadAndMergeJsons(documents, resolver);

            jsonSerializerOptions ??= JsonSerializerOptions.Default;
            return new Localized<T>(documents.Select(ConvertToT));

            KeyValuePair<CultureInfo, T> ConvertToT(KeyValuePair<string, LoadEntry> p)
            {
                var entry = p.Value;
                var content = JsonSerializer.Deserialize<T>(entry.Json, jsonSerializerOptions)!;
                return KeyValuePair.Create(entry.CultureInfo, content);
            }
        }
        finally
        {
            foreach (var entry in documents)
                entry.Value.Json.Dispose();
        }
    }

    internal static ImmutableArray<KeyValuePair<string, T>> ToDataField<T>(TinyDictionary<string, T> data)
    {
        if (data.Count == 0) return default;
        return ImmutableCollectionsMarshal.AsImmutableArray(data.ToArray());
    }

    internal static ImmutableArray<CultureInfo> GetSupportedCulturesOrThrow()
    {
        var cultures = _supportedCultures;
        if (cultures.IsDefault)
        {
            ThrowHelper.InvalidOperation("Must set SupportedCultures first.");
        }

        return cultures;
    }

    private static CultureInfo GetSupportedCultureNoCache(string name)
    {
        if (name.Length == 0)
            return CultureInfo.InvariantCulture;

        return GetExactSupportCulture(name)
            ?? GetSupportedCultureByParent(name);
    }

    private static CultureInfo? GetExactSupportCulture(string name)
    {
        foreach (var cult in _supportedCultures)
        {
            if (name.Equals(cult.Name, StringComparison.OrdinalIgnoreCase))
                return cult;
        }

        return null;
    }

    private static CultureInfo GetSupportedCultureByParent(string name)
    {
        CultureInfo? culture;
        try
        {
            culture = CultureInfo.GetCultureInfo(name);
            culture = GetSupportedCultureNoCache(culture.Parent.Name);
        }
        catch (CultureNotFoundException)
        {
            culture = CultureInfo.InvariantCulture;
        }

        return culture;
    }

    private sealed class CultureCache : RareWriteFactoryCache<string, CultureInfo>
    {
        protected override CultureInfo Create(string key)
        {
            // We don't want recursive re-entry into this method.
            return GetSupportedCultureNoCache(key);
        }
    }
}

file sealed class LoadEntry
{
    public required JsonDocument Json { get; set; }
    public required CultureInfo CultureInfo { get; set; }
    public bool IsMerged { get; set; }

    internal static LoadEntry LoadJson(StreamResolver resolver, CultureInfo cultureInfo)
    {
        using var stream = resolver(cultureInfo);
        return new LoadEntry
        {
            Json = JsonDocument.Parse(stream, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip }),
            CultureInfo = cultureInfo
        };
    }

    internal static void LoadAndMergeJsons(TinyDictionary<string, LoadEntry> store, StreamResolver resolver)
    {
        var cultures = Localizer.GetSupportedCulturesOrThrow();

        // Load all JSON files
        store[string.Empty] = LoadJson(resolver, CultureInfo.InvariantCulture);
        foreach (var culture in cultures)
        {
            try { store[culture.Name] = LoadJson(resolver, culture); }
            catch (FileNotFoundException) { /* This is fine. */ }
        }

        // Then merge them appropriately
        foreach (var culture in cultures)
        {
            if (store.TryGetValue(culture.Name, out var doc))
            {
                Merge(doc, culture);
            }
        }

        void Merge(LoadEntry doc, CultureInfo cultureInfo)
        {
            if (doc.IsMerged) return;
            if (cultureInfo == CultureInfo.InvariantCulture) return;

            if (store.TryGetValue(cultureInfo.Parent.Name, out var parentDoc))
            {
                Merge(parentDoc, cultureInfo.Parent);

                var newDoc = JsonMerge.Merge(parentDoc.Json, doc.Json);
                doc.Json.Dispose();
                doc.Json = newDoc;
            }

            doc.IsMerged = true;
        }
    }
}
