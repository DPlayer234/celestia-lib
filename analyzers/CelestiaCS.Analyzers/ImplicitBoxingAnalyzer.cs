using System.Collections.Immutable;
using CelestiaCS.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CelestiaCS.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ImplicitBoxingAnalyzer : DiagnosticAnalyzer
{
    // This analyzer currently only checks for two specific implicit boxing cases:
    // - Receiver of a method call is boxed
    // - Argument to an invocation is boxed
    // It also has a hard-coded list of method names to ignore to avoid most unfixable warnings.

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        Diagnostics.Performance.ImplicitBoxing
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterSyntaxNodeAction(
            action: CheckArgument,
            syntaxKinds: SyntaxKind.Argument);

        context.RegisterSyntaxNodeAction(
            action: CheckInvocation,
            syntaxKinds: SyntaxKind.InvocationExpression);
    }

    private static void CheckArgument(SyntaxNodeAnalysisContext ctx)
    {
        var node = (ArgumentSyntax)ctx.Node;
        if (node.Parent!.Parent is InvocationExpressionSyntax invocation && IsIgnoredCall(invocation)) return;

        var typeInfo = ctx.SemanticModel.GetTypeInfo(node.Expression);
        TryReportBoxing(in ctx, node, typeInfo);
    }

    private static void CheckInvocation(SyntaxNodeAnalysisContext ctx)
    {
        var node = (InvocationExpressionSyntax)ctx.Node;
        if (IsIgnoredCall(node)) return;
        if (node.Expression.Unwrap() is not MemberAccessExpressionSyntax access) return;

        var receiver = ctx.SemanticModel.GetTypeInfo(access.Expression);
        TryReportBoxing(in ctx, node, receiver);
    }

    private static bool IsIgnoredCall(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression.Unwrap() switch
        {
            MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText
                is "HasFlag" or "ThrowIfNull" or "WriteLine" or "Write" or "Format"
                or "FromSuccess" or "Cast"
                or "EqualTo" or "Within" or "GreaterThan" or "LessThan" or "GreaterThanOrEqualTo" or "LessThanOrEqualTo" or "Key" or "WithValue",
            ObjectCreationExpressionSyntax => false,
            _ => false
        };
    }

    private static void TryReportBoxing(in SyntaxNodeAnalysisContext ctx, CSharpSyntaxNode node, TypeInfo typeInfo)
    {
        if (IsBoxingConversion(ctx, typeInfo))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Performance.ImplicitBoxing, node.GetLocation(), typeInfo.Type?.Name, typeInfo.ConvertedType?.Name));
        }
    }

    private static bool IsBoxingConversion(in SyntaxNodeAnalysisContext ctx, TypeInfo typeInfo)
    {
        return typeInfo.Type is not (null or IErrorTypeSymbol)
            && typeInfo.ConvertedType is not (null or IErrorTypeSymbol)
            && ctx.Compilation.ClassifyConversion(typeInfo.Type, typeInfo.ConvertedType).IsBoxing;
    }
}
