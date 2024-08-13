using System.Collections.Generic;
using System.Collections.Immutable;

namespace CelestiaCS.Analyzers.Model;

public readonly record struct ValueArray<T>(ImmutableArray<T> Array)
{
    public readonly bool Equals(ValueArray<T> other)
    {
        var self = Array;
        var that = other.Array;
        if (self == that) return true;
        if (self.Length != that.Length) return false;

        if (typeof(T).IsValueType)
        {
            for (int i = 0; i < self.Length; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(self[i], that[i]))
                    return false;
            }
        }
        else
        {
            var eq = EqualityComparer<T>.Default;
            for (int i = 0; i < self.Length; i++)
            {
                if (!eq.Equals(self[i], that[i]))
                    return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        const uint Magic = 3266489917U;

        uint code = Magic;
        foreach (var item in Array)
        {
            code = code * 374761393U ^ (uint)(item?.GetHashCode() ?? 0) * Magic;
        }

        return (int)code;
    }

    public static implicit operator ValueArray<T>(ImmutableArray<T> array) => new(array);
}
