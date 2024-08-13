using System;

namespace CelestiaCS.Analyzers.Model;

[Flags]
public enum MethodModifiers
{
    None = 0,
    Virtual = 0x1,
    Override = 0x2,
    Sealed = 0x4,
    Partial = 0x8,
}

public static class MethodModifiersExtensions
{
    public static string ToCSharpPrefix(this MethodModifiers modifiers)
    {
        return string.Concat
        (
            modifiers.HasFlag(MethodModifiers.Sealed) ? "sealed " : string.Empty,
            modifiers.HasFlag(MethodModifiers.Virtual) ? "virtual " : string.Empty,
            modifiers.HasFlag(MethodModifiers.Override) ? "override " : string.Empty,
            modifiers.HasFlag(MethodModifiers.Partial) ? "partial " : string.Empty
        );
    }
}
