using System.Collections.Generic;
using System.Runtime.InteropServices;
using CelestiaCS.Lib.Collections;

namespace CelestiaCS.Lib.Linq;

/// <summary>
/// Provides additional methods for dictionary of collections.
/// </summary>
public static class DictionaryOfCollectionExtensions
{
    /// <inheritdoc cref="AddToValue{TKey, TValue, TAlloc}(Dictionary{TKey, ValueList{TValue, TAlloc}}, TKey, TValue)"/>
    public static void AddToValue<TKey, TValue>(this Dictionary<TKey, ValueList<TValue>> dict, TKey key, TValue value)
        where TKey : notnull
    {
        // If not already contained, adds a default(ValueList<TValue>) and returns a ref to that
        CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out _).Add(value);
    }

    /// <inheritdoc cref="RemoveFromValue{TKey, TValue, TAlloc}(Dictionary{TKey, ValueList{TValue, TAlloc}}, TKey, TValue)"/>
    public static void RemoveFromValue<TKey, TValue>(this Dictionary<TKey, ValueList<TValue>> dict, TKey key, TValue value)
        where TKey : notnull
    {
        CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out _).Remove(value);
    }

    /// <summary>
    /// Adds an item to the list contained at the specified key.
    /// </summary>
    /// <typeparam name="TKey"> The type of the key. </typeparam>
    /// <typeparam name="TValue"> The type of the list items. </typeparam>
    /// <typeparam name="TAlloc"> The array allocator used by the lists. </typeparam>
    /// <param name="dict"> The dictionary of lists. </param>
    /// <param name="key"> The key. </param>
    /// <param name="value"> The value to add to the list. </param>
    public static void AddToValue<TKey, TValue, TAlloc>(this Dictionary<TKey, ValueList<TValue, TAlloc>> dict, TKey key, TValue value)
        where TKey : notnull
        where TAlloc : IArrayAllocator
    {
        CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out _).Add(value);
    }

    /// <summary>
    /// Removes an item from the list contained at the specified key.
    /// </summary>
    /// <typeparam name="TKey"> The type of the key. </typeparam>
    /// <typeparam name="TValue"> The type of the list items. </typeparam>
    /// <typeparam name="TAlloc"> The array allocator used by the lists. </typeparam>
    /// <param name="dict"> The dictionary of lists. </param>
    /// <param name="key"> The key. </param>
    /// <param name="value"> The value to remove from the list. </param>
    public static void RemoveFromValue<TKey, TValue, TAlloc>(this Dictionary<TKey, ValueList<TValue, TAlloc>> dict, TKey key, TValue value)
        where TKey : notnull
        where TAlloc : IArrayAllocator
    {
        CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out _).Remove(value);
    }
}
