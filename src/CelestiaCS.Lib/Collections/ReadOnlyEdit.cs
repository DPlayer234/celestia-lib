using System;
using System.Collections.Generic;
using System.Linq;
using CelestiaCS.Lib.Linq;

namespace CelestiaCS.Lib.Collections;

/// <summary>
/// Utilities to create modified copies of read-only lists.
/// </summary>
public static class ReadOnlyEdit
{
    /// <summary> Appends an item to the end of the list. </summary>
    /// <typeparam name="T"> The type of elements. </typeparam>
    /// <param name="source"> The source list. </param>
    /// <param name="item"> The item to add. </param>
    /// <returns> A modified copy of the list. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="source"/> is null. </exception>
    public static IReadOnlyList<T> Add<T>(IReadOnlyList<T> source, T item)
    {
        ArgumentNullException.ThrowIfNull(source);

        T[] array = new T[source.Count + 1];
        CopyTo(source, array, 0);

        array[^1] = item;
        return ReadOnlyList.Create(array);
    }

    /// <summary> Appends items to the end of the list. </summary>
    /// <typeparam name="T"> The type of elements. </typeparam>
    /// <param name="source"> The source list. </param>
    /// <param name="append"> The items to append. </param>
    /// <returns> A modified copy of the list. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="source"/> or <paramref name="append"/> is null. </exception>
    public static IReadOnlyList<T> AddRange<T>(IReadOnlyList<T> source, IEnumerable<T> append)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(append);

        T[] array;
        if (append is ICollection<T> appendCollection)
        {
            array = new T[source.Count + appendCollection.Count];
            CopyTo(source, array, 0);
            appendCollection.CopyTo(array, source.Count);
        }
        else
        {
            ValueList<T> temp = [];
            temp.AddRange(source);
            temp.AddRange(append);
            array = temp.DrainToArray();
        }

        return ReadOnlyList.Create(array);
    }

    /// <summary> Removes an item from the list. </summary>
    /// <typeparam name="T"> The type of elements. </typeparam>
    /// <param name="source"> The source list. </param>
    /// <param name="item"> The items to remove. </param>
    /// <returns> A modified copy of the list or <paramref name="source"/> if <paramref name="item"/> wasn't found. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="source"/> is null. </exception>
    public static IReadOnlyList<T> Remove<T>(IReadOnlyList<T> source, T item)
    {
        ArgumentNullException.ThrowIfNull(source);

        int index = source.IndexOf(item);
        if (index >= 0)
        {
            return RemoveAt(source, index);
        }

        return source;
    }

    /// <summary> Removes the item at the specified index from the list. </summary>
    /// <typeparam name="T"> The type of elements. </typeparam>
    /// <param name="source"> The source list. </param>
    /// <param name="index"> The index of the item to remove. </param>
    /// <returns> A modified copy of the list. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="source"/> is null. </exception>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="index"/> wasn't within the range of the list. </exception>
    public static IReadOnlyList<T> RemoveAt<T>(IReadOnlyList<T> source, int index)
    {
        ArgumentNullException.ThrowIfNull(source);

        int count = source.Count;
        if ((uint)index >= (uint)count) ThrowHelper.ArgumentOutOfRange_IndexMustBeLess();
        if (count <= 1) return ReadOnlyList<T>.Empty;

        T[] copy = new T[count - 1];

        for (int i = 0; i < index; i++)
        {
            copy[i] = source[i];
        }

        for (int i = index; i < copy.Length; i++)
        {
            copy[i] = source[i + 1];
        }

        return ReadOnlyList.Create(copy);
    }

    /// <summary> Searches for an item in the list and replaces it with another. </summary>
    /// <typeparam name="T"> The type of elements. </typeparam>
    /// <param name="source"> The source list. </param>
    /// <param name="search"> The items to search for. </param>
    /// <param name="replacement"> The item to replace <paramref name="search"/> with. </param>
    /// <returns> A modified copy of the list or <paramref name="source"/> if <paramref name="search"/> wasn't found. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="source"/> is null. </exception>
    public static IReadOnlyList<T> Replace<T>(IReadOnlyList<T> source, T search, T replacement)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (EqualityComparer<T>.Default.Equals(search, replacement)) return source;

        int index = source.IndexOf(search);
        if (index >= 0)
        {
            T[] copy = source.ToArray();
            copy[index] = replacement;
            return ReadOnlyList.Create(copy);
        }

        return source;
    }

    /// <summary> Searches for an item in the list and replaces it with another. If successful, <paramref name="source"/> is replaced with a modified copy. </summary>
    /// <typeparam name="T"> The type of elements. </typeparam>
    /// <param name="source"> The source list. </param>
    /// <param name="search"> The items to search for. </param>
    /// <param name="replacement"> The item to replace <paramref name="search"/> with. </param>
    /// <returns> Whether an element was replaced. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="source"/> is null. </exception>
    public static bool TryReplace<T>(ref IReadOnlyList<T> source, T search, T replacement)
    {
        IReadOnlyList<T> local = source;
        IReadOnlyList<T> result;
        if ((result = Replace(local, search, replacement)) != local)
        {
            source = result;
            return true;
        }

        return false;
    }

    private static void CopyTo<T>(IReadOnlyList<T> source, T[] array, int index)
    {
        if (source is ICollection<T> collection)
        {
            collection.CopyTo(array, index);
        }
        else
        {
            int count = source.Count;
            for (int i = 0; i < count; i++)
            {
                array[index++] = source[i];
            }
        }
    }
}
