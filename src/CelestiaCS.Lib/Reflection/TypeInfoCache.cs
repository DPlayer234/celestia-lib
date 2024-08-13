using System;

namespace CelestiaCS.Lib.Reflection;

/// <summary>
/// Provides runtime constants regarding type info.
/// </summary>
/// <typeparam name="T"> Info for which type. </typeparam>
public static class TypeInfoCache<T>
{
    /// <summary>
    /// Runtime constant value equivalent to <see cref="Type.IsSealed"/>.
    /// </summary>
    public static bool IsSealed { get; } = typeof(T).IsSealed;
}
