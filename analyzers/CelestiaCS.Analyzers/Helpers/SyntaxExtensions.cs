using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CelestiaCS.Analyzers.Helpers;

internal static class SyntaxExtensions
{
    public static SyntaxNode Unwrap(this SyntaxNode expression)
    {
        while (expression is ParenthesizedExpressionSyntax p)
            expression = p.Expression;

        return expression;
    }

    public static ExpressionSyntax Unwrap(this ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax p)
            expression = p.Expression;

        return expression;
    }
}
