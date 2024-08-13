using System.Runtime.CompilerServices;

namespace CelestiaCS.Lib.Memory;

#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0051 // Remove unused private members

/// <summary> Defines an inline-array type with 4 elements. </summary>
/// <typeparam name="T"> The type of the elements. </typeparam>
[InlineArray(4)]
public struct InlineArray4<T>
{
    private T _element0;
}

/// <summary> Defines an inline-array type with 8 elements. </summary>
/// <typeparam name="T"> The type of the elements. </typeparam>
[InlineArray(8)]
public struct InlineArray8<T>
{
    private T _element0;
}

/// <summary> Defines an inline-array type with 16 elements. </summary>
/// <typeparam name="T"> The type of the elements. </typeparam>
[InlineArray(16)]
public struct InlineArray16<T>
{
    private T _element0;
}

/// <summary> Defines an inline-array type with 32 elements. </summary>
/// <typeparam name="T"> The type of the elements. </typeparam>
[InlineArray(32)]
public struct InlineArray32<T>
{
    private T _element0;
}
