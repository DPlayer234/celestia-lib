using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CelestiaCS.Lib.Collections.Internal;

public static class CollectionOfTImplHelper
{
    public static void ValidateCopyToArguments(int count, Array array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);

        if ((uint)arrayIndex > (uint)array.Length)
            ThrowHelper.ArgumentOutOfRange(nameof(arrayIndex));
        if ((uint)count > (uint)(array.Length - arrayIndex))
            ThrowHelper.Argument(nameof(array), "Not enough space in the destination array.");
    }

    public static void CopyTo<T>(ICollection<T> source, T[] array, int arrayIndex)
    {
        int count = source.Count;
        ValidateCopyToArguments(count, array, arrayIndex);
        if (count == 0) return;

        foreach (var item in source)
            array[arrayIndex++] = item;
    }

    public static void CopyTo(Array? source, int count, Array array, int arrayIndex)
    {
        ValidateCopyToArguments(count, array, arrayIndex);
        if (count == 0) return;

        Debug.Assert(source != null, "Source must not be null if count != 0.");
        Array.Copy(source, 0, array, arrayIndex, count);
    }
}
