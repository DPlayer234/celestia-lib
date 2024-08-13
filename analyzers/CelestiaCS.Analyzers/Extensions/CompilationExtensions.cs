using System;
using Microsoft.CodeAnalysis;

namespace CelestiaCS.Analyzers.Extensions;

public static class CompilationExtensions
{
    public static INamedTypeSymbol LookupType(this Compilation compilation, string fqName)
    {
        var type = compilation.GetTypeByMetadataName(fqName);
        return type ?? throw new InvalidOperationException($"Unknown type {fqName}.");
    }

    public static IncrementalValuesProvider<T> WhereNotNull<T>(this IncrementalValuesProvider<T?> provider) where T : notnull
    {
        return provider.Where(x => x != null)!;
    }
}
