using System.Collections.Generic;
using System.Collections.Immutable;
using CelestiaCS.Analyzers.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CelestiaCS.Analyzers.Extensions;

public static class SymbolHelpers
{
    public static SymbolDisplayFormat FullyQualifiedFormatWithNullable { get; }
        = SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public static SymbolDisplayFormat ShortDiagnosticFormat { get; }
        = SymbolDisplayFormat.CSharpShortErrorMessageFormat.RemoveMemberOptions(SymbolDisplayMemberOptions.IncludeParameters);

    public static AttributeData? FindAttributeByType(this ISymbol symbol, INamedTypeSymbol attributeType)
    {
        foreach (var attr in symbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType))
                return attr;
        }

        return null;
    }

    public static IEnumerable<AttributeData> FindAttributesByType(this ISymbol symbol, INamedTypeSymbol attributeType)
    {
        foreach (var attr in symbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType))
                yield return attr;
        }
    }

    public static ImmutableArray<ISymbol> LookupMembers(this ITypeSymbol type, string memberName)
    {
        var result = ImmutableArray<ISymbol>.Empty;

        var t = type;
        while (t != null)
        {
            // Generally only expecting one or two types in the hierarchy to hold this
            result = result.AddRange(t.GetMembers(memberName));
            t = t.BaseType;
        }

        return result;
    }

    public static bool Implements(this ITypeSymbol type, INamedTypeSymbol interfaceType)
    {
        var comparer = SymbolEqualityComparer.Default;
        foreach (var item in type.AllInterfaces)
        {
            if (comparer.Equals(item, interfaceType))
                return true;
        }

        return false;
    }

    public static bool DerivesFrom(this ITypeSymbol type, ITypeSymbol baseType)
    {
        var comparer = SymbolEqualityComparer.Default;
        var parent = type.BaseType;
        while (parent != null)
        {
            if (comparer.Equals(parent, baseType))
                return true;

            parent = parent.BaseType;
        }

        return false;
    }

    public static bool IsPartial(this TypeDeclarationSyntax symbol)
    {
        foreach (SyntaxToken token in symbol.Modifiers)
        {
            if (token.IsKind(SyntaxKind.PartialKeyword))
                return true;
        }

        return false;
    }

    public static bool IsPublic(this INamedTypeSymbol type)
    {
        while (type.DeclaredAccessibility == Accessibility.Public)
        {
            type = type.ContainingType;
            if (type == null) return true;
        }

        return false;
    }

    public static bool IsPrimaryDeclaration(this SyntaxNode syntax, ISymbol symbol)
    {
        var decl = symbol.DeclaringSyntaxReferences;
        if (decl.Length <= 1) return true;

        var first = decl[0];
        return first.SyntaxTree == syntax.SyntaxTree
            && first.Span == syntax.Span;
    }

    public static string? ToCSharpString(this Accessibility accessibility)
    {
        return accessibility switch
        {
            Accessibility.Private => "private",
            Accessibility.ProtectedAndInternal => "private protected",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.Public => "public",
            _ => null
        };
    }

    public static string? ToCSharpPrefix(this RefKind kind) => kind switch
    {
        RefKind.None => null,
        RefKind.Ref => "ref ",
        RefKind.Out => "out ",
        RefKind.In => "in ",
        _ => "unknown "
    };

    public static bool TryGetParameter<T>(this AttributeData attr, int index, out T value)
    {
        var args = attr.ConstructorArguments;
        if ((uint)index < (uint)args.Length && args[index].Value is T iValue)
        {
            value = iValue;
            return true;
        }

        value = default!;
        return false;
    }

    public static bool TryGetNamedParameter<T>(this AttributeData attr, string name, out T value)
    {
        foreach (var item in attr.NamedArguments)
        {
            if (item.Key == name && item.Value.Value is T iValue)
            {
                value = iValue;
                return true;
            }
        }

        value = default!;
        return false;
    }

    public static bool TryGetNullableParameter<T>(this AttributeData attr, int index, out T? value)
        where T : class
    {
        var args = attr.ConstructorArguments;
        if ((uint)index < (uint)args.Length)
        {
            if (args[index].Value is T iValue)
            {
                value = iValue;
                return true;
            }
            else if (args[index].Value is null)
            {
                value = default!;
                return true;
            }
        }

        value = default!;
        return false;
    }

    public static MethodModifiers GetMethodModifiers(this IMethodSymbol method)
    {
        MethodModifiers result = MethodModifiers.None;
        if (method.IsVirtual) result |= MethodModifiers.Virtual;
        if (method.IsOverride) result |= MethodModifiers.Override;
        if (method.IsSealed) result |= MethodModifiers.Sealed;
        if (method.IsPartialDefinition) result |= MethodModifiers.Partial;
        return result;
    }

    public static bool IsNullableT(this ITypeSymbol type)
    {
        return type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
    }

    public static ITypeSymbol UnwrapNullableT(this ITypeSymbol type)
    {
        return type.IsNullableT()
            ? ((INamedTypeSymbol)type).TypeArguments[0]
            : type;
    }
}
