using System;
using System.Collections.Immutable;

namespace CelestiaCS.Lib.Events;

internal static class IntlShared
{
    public static readonly Func<object> NewObj = () => new object();

    public static class Update<T>
    {
        public static readonly Func<ImmutableArray<T>, T, ImmutableArray<T>> Add = (l, a) => l.IsDefault ? [a] : l.Add(a);
        public static readonly Func<ImmutableArray<T>, T, ImmutableArray<T>> Remove = (l, a) => l.IsDefault ? l : l.Remove(a);
    }
}
