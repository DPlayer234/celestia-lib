using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CelestiaCS.Lib.Linq;

/// <summary>
/// Provides extension methods for dictionary classes.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// Adds or updates an existing value in a concurrent dictionary.
    /// This is intended to be used when there doesn't need to be a difference between adding and updating.
    /// </summary>
    /// <typeparam name="TKey"> The type of the dictionary keys. </typeparam>
    /// <typeparam name="TValue"> The type of the dictionary values. </typeparam>
    /// <param name="dict"> The dictionary to update. </param>
    /// <param name="key"> The key to be added or whose value should be updated. </param>
    /// <param name="value"> The value to insert. </param>
    /// <returns> The new value for the key. </returns>
    public static TValue AddOrUpdate<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, TValue value)
        where TKey : notnull
    {
        return dict.AddOrUpdate(
            key,
            static (key, arg) => arg,
            static (key, old, arg) => arg,
            value);
    }

    /// <summary>
    /// Enumerate over the keys of a dictionary through its own <see cref="IEnumerable{T}"/> implementation.
    /// This does not access the <see cref="IDictionary{TKey, TValue}.Keys"/> property.
    /// </summary>
    /// <typeparam name="TKey"> The key type. </typeparam>
    /// <typeparam name="TValue"> The value type. </typeparam>
    /// <param name="dictionary"> The dictionary to enumerate. </param>
    /// <returns> An enumerable over the dictionary keys. </returns>
    public static IEnumerable<TKey> Keys<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary)
    {
        return dictionary.Select(p => p.Key);
    }

    /// <summary>
    /// Enumerate over the values of a dictionary through its own <see cref="IEnumerable{T}"/> implementation.
    /// This does not access the <see cref="IDictionary{TKey, TValue}.Values"/> property.
    /// </summary>
    /// <typeparam name="TKey"> The key type. </typeparam>
    /// <typeparam name="TValue"> The value type. </typeparam>
    /// <param name="dictionary"> The dictionary to enumerate. </param>
    /// <returns> An enumerable over the dictionary values. </returns>
    public static IEnumerable<TValue> Values<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary)
    {
        return dictionary.Select(p => p.Value);
    }
}
