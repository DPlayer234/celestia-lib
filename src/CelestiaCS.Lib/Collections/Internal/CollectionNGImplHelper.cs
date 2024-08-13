using System;
using System.Collections;
using System.Diagnostics;

namespace CelestiaCS.Lib.Collections.Internal;

public static class CollectionNGImplHelper
{
    public static void ValidateCopyToArguments(int count, Array array, int index)
    {
        ArgumentNullException.ThrowIfNull(array);

        if (array.Rank != 1)
            ThrowHelper.Argument(nameof(array), "Multi-dimensional arrays not supported.");

        int lb = array.GetLowerBound(0);
        uint length = (uint)array.LongLength;
        uint offset = (uint)(index - lb);
        if (index < lb || offset > length)
            ThrowHelper.ArgumentOutOfRange(nameof(index));

        if ((uint)count > (length - offset))
            ThrowHelper.Argument(nameof(array), "Not enough space in the destination array.");
    }

    public static void ThrowInvalidArrayType()
    {
        ThrowHelper.Argument("array", "The target array type is not compatible with the type of items in the collection.");
    }

    public static void CopyTo(ICollection source, Array array, int index)
    {
        int count = source.Count;
        ValidateCopyToArguments(count, array, index);
        if (count == 0) return;

        try
        {
            foreach (var item in source)
                array.SetValue(item, index++);
        }
        catch (InvalidCastException)
        {
            ThrowInvalidArrayType();
        }
    }

    public static void CopyTo(Array? source, int count, Array array, int index)
    {
        ValidateCopyToArguments(count, array, index);
        if (count == 0) return;

        Debug.Assert(source != null, "Source must not be null if count != 0.");
        Array.Copy(source, 0, array, index, count);
    }
}
