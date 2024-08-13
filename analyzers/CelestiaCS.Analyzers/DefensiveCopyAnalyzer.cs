using System;
using System.Collections.Immutable;
using CelestiaCS.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace CelestiaCS.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DefensiveCopyAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        Diagnostics.Correctness.MutableOverReadOnlyField,
        Diagnostics.Correctness.MutableOverReadOnlyRef,
        Diagnostics.Correctness.MutableOverMethodReturn,
        Diagnostics.Correctness.MutableOverPropertyReturn,
        Diagnostics.Correctness.MutableOverImplicitClonedParam,
        Diagnostics.Correctness.MutableOverConditionalNullable,
        Diagnostics.Correctness.MutableOverTemporary,
        Diagnostics.Correctness.MutableOverForEach,
        Diagnostics.Correctness.MutableOverUnknown
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterOperationAction(
            action: CheckInvocation,
            operationKinds: OperationKind.Invocation);

        context.RegisterOperationAction(
            action: CheckProperty,
            operationKinds: OperationKind.PropertyReference);

        context.RegisterOperationAction(
            action: CheckImplicitIndexer,
            operationKinds: OperationKind.ImplicitIndexerReference);

        context.RegisterOperationAction(
            action: CheckEvent,
            operationKinds: OperationKind.EventReference);
    }

    private static void CheckInvocation(OperationAnalysisContext ctx)
    {
        var invoke = (IInvocationOperation)ctx.Operation;
        var receiver = invoke.Instance;
        if (receiver == null || SkipReceiver(receiver)) return;

        CheckMethod(in ctx, receiver, invoke.TargetMethod);
    }

    private static void CheckProperty(OperationAnalysisContext ctx)
    {
        var reference = (IPropertyReferenceOperation)ctx.Operation;
        var receiver = reference.Instance;
        if (receiver == null || SkipReceiver(receiver)) return;

        CheckProperty(in ctx, receiver, reference.Property);
    }

    private static void CheckImplicitIndexer(OperationAnalysisContext ctx)
    {
        var reference = (IImplicitIndexerReferenceOperation)ctx.Operation;
        var receiver = reference.Instance;
        if (receiver == null || SkipReceiver(receiver)) return;

        CheckSymbol(in ctx, receiver, reference.LengthSymbol);
        CheckSymbol(in ctx, receiver, reference.IndexerSymbol);
    }

    private static void CheckEvent(OperationAnalysisContext ctx)
    {
        var reference = (IEventReferenceOperation)ctx.Operation;
        var receiver = reference.Instance;
        if (receiver == null || SkipReceiver(receiver)) return;

        var @event = reference.Event;
        var method = reference.Parent switch
        {
            IEventAssignmentOperation assign => assign.Adds ? @event.AddMethod : @event.RemoveMethod,
            IRaiseEventOperation => @event.RaiseMethod,
            _ => null
        };

        if (method == null) return;

        CheckMethod(in ctx, receiver, method);
    }

    private static void CheckSymbol(in OperationAnalysisContext ctx, IOperation receiver, ISymbol symbol)
    {
        switch (symbol)
        {
            case IMethodSymbol method:
                CheckMethod(in ctx, receiver, method);
                break;
            case IPropertySymbol property:
                CheckProperty(in ctx, receiver, property);
                break;
        }
    }

    private static void CheckMethod(in OperationAnalysisContext ctx, IOperation receiver, IMethodSymbol method)
    {
        if (method.IsReadOnly || method.IsStatic || method.IsExtensionMethod) return;

        var type = ResolveReceiver(receiver.Type, method.ContainingType);
        if (!IsProblematicType(type)) return;

        ReportReceiverProblem(in ctx, receiver, method);
    }

    private static void CheckProperty(in OperationAnalysisContext ctx, IOperation receiver, IPropertySymbol property)
    {
        if (property.IsStatic) return;

        var type = ResolveReceiver(receiver.Type, property.ContainingType);
        if (!IsProblematicType(type)) return;

        var method = property.GetMethod;
        if (method == null || method.IsReadOnly) return;

        ReportReceiverProblem(in ctx, receiver, property);
    }

    private static void ReportReceiverProblem(in OperationAnalysisContext ctx, IOperation receiver, ISymbol symbol)
    {
        var problem = FindReceiverProblem(in ctx, receiver);
        if (problem == Problem.Ok) return;

        var diagnostic = problem switch
        {
            Problem.ReadOnlyField => Diagnostics.Correctness.MutableOverReadOnlyField,
            Problem.ReadOnlyRef => Diagnostics.Correctness.MutableOverReadOnlyRef,
            Problem.MethodReturn => Diagnostics.Correctness.MutableOverMethodReturn,
            Problem.PropertyReturn => Diagnostics.Correctness.MutableOverPropertyReturn,
            Problem.MutableOverConditionalNullable => Diagnostics.Correctness.MutableOverConditionalNullable,
            Problem.Temporary => Diagnostics.Correctness.MutableOverTemporary,
            Problem.ForEach => Diagnostics.Correctness.MutableOverForEach,
            Problem.ImplicitClonedParam => Diagnostics.Correctness.MutableOverImplicitClonedParam,
            _ => Diagnostics.Correctness.MutableOverUnknown,
        };

        var syntax = GetReportNode(receiver);
        ctx.ReportDiagnostic(Diagnostic.Create
        (
            diagnostic,
            syntax.GetLocation(),
            TruncateAtNewLine(syntax.ToString()),
            symbol.ToDisplayString(SymbolHelpers.ShortDiagnosticFormat)
        ));
    }

    private static Problem FindReceiverProblem(in OperationAnalysisContext ctx, IOperation receiver)
    {
        return FindReceiverProblem(in ctx, ref receiver);
    }

    private static Problem FindReceiverProblem(in OperationAnalysisContext ctx, ref IOperation receiver)
    {
    Next:
        switch (receiver)
        {
            // Receiver is static
            case null:
            {
                return Problem.Ok;
            }

            case IInvocationOperation invoke:
            {
                return FindMethodProblem(invoke.TargetMethod);
            }

            case IPropertyReferenceOperation reference:
            {
                return FindPropertyProblem(reference.Property);
            }

            case IImplicitIndexerReferenceOperation indexer:
            {
                return FindSymbolProblem(indexer.IndexerSymbol);
            }

            case IFieldReferenceOperation reference:
            {
                IFieldSymbol field = reference.Field;

                // If the field type is read-only, it is never a problem.
                if (!IsProblematicType(field.Type)) return Problem.Ok;

                RefKind refKind = field.RefKind;
                if (refKind is RefKind.Ref) return Problem.Ok;
                if (refKind is RefKind.RefReadOnly) return Problem.ReadOnlyRef;

                var instance = reference.Instance;
                var method = AsMethod(ctx.ContainingSymbol);

                if (IsThis(instance) && method != null &&
                    (method.MethodKind is MethodKind.Constructor || method.IsInitOnly) &&
                    SymbolEqualityComparer.Default.Equals(method.ContainingType, field.ContainingType))
                {
                    // If this field is directly accessed on `this` within a constructor or init-only method
                    // we need to treat the field as mutable. Since we now also know the entire remaining tree,
                    // that is just `this.<field>`, and both are mutable, this isn't a problem.
                    return Problem.Ok;
                }

                if (instance is null && method != null &&
                    method.MethodKind is MethodKind.StaticConstructor &&
                    SymbolEqualityComparer.Default.Equals(method.ContainingType, field.ContainingType))
                {
                    // Same as the previous case but for static constructors.
                    return Problem.Ok;
                }

                // A mutable field at this point is a problem.
                if (field.IsReadOnly) return Problem.ReadOnlyField;

                // ... and so is a problematic nested receiver.
                if (instance != null)
                {
                    receiver = instance;
                    var problem = FindReceiverProblem(in ctx, ref receiver);
                    if (problem != Problem.Ok) return problem;

                    // Also, if we access a mutable field that is indirectly on `this` (i.e. through a chain of also mutable fields)
                    // but the method is marked as read-only, we need to treat the whole thing as read-only, which is a problem too.
                    if (IsThis(receiver) && method != null && method.IsReadOnly) return Problem.ReadOnlyField;
                }

                return Problem.Ok;
            }

            case IParameterReferenceOperation reference:
            {
                IParameterSymbol param = reference.Parameter;
                return param.RefKind switch
                {
                    RefKind.None when IsProblematicType(param.Type) => Problem.ImplicitClonedParam,
                    RefKind.In or RefKind.RefReadOnlyParameter when IsProblematicType(param.Type) => Problem.ReadOnlyRef,
                    _ => Problem.Ok
                };
            }

            case ILocalReferenceOperation reference:
            {
                ILocalSymbol local = reference.Local;
                return local.RefKind switch
                {
                    RefKind.None when local.IsForEach && IsProblematicType(local.Type) => Problem.ForEach,
                    RefKind.RefReadOnly when IsProblematicType(local.Type) => Problem.ReadOnlyRef,
                    _ => Problem.Ok
                };
            }

            case IConditionalAccessInstanceOperation instance:
            {
                var target = FindInstance(instance);
                if (target == null) return Problem.Unknown;

                var type = target.Type;
                if (type == null) return Problem.Unknown;

                if (type.IsNullableT())
                {
                    return IsProblematicType(type.UnwrapNullableT()) ? Problem.MutableOverConditionalNullable : Problem.Ok;
                }

                receiver = target;
                goto Next;
            }

            case IInlineArrayAccessOperation access:
            {
                // Inline array access matches whatever its parent is.
                receiver = access.Instance;
                goto Next;
            }

            case IArrayElementReferenceOperation or IConditionalAccessOperation:
            {
                // 1. Arrays are mutable.
                // 2. You'd only hit a conditional access here if you parenthesized it, i.e.: (Int?.Value).GetHashCode()
                //    This would only allow you to access methods on Nullable<T> and would operate on a copy anyways.
                return Problem.Ok;
            }

            case IInstanceReferenceOperation instance:
            {
                // `this` is usually not a problem outside of pattern inputs.
                // Also, the compiler already handles the basic cases here.
                if (instance.ReferenceKind != InstanceReferenceKind.PatternInput) return Problem.Ok;

                var originalValue = FindPatternInstance(instance);
                if (originalValue == null) return Problem.Unknown;

                receiver = originalValue;
                goto Next;
            }

            case IObjectCreationOperation creation:
            {
                var type = creation.Type;
                if (type == null) return Problem.Unknown;
                return IsProblematicType(type) ? Problem.MethodReturn : Problem.Ok;
            }

            case IFunctionPointerInvocationOperation invoke:
            {
                return invoke.Target.Type is IFunctionPointerTypeSymbol fPtr
                    ? FindMethodProblem(fPtr.Signature)
                    : Problem.Unknown;
            }

            default:
            {
                var type = receiver.Type;
                if (type == null) return Problem.Unknown;
                if (!IsProblematicType(type)) return Problem.Ok;

                var children = receiver.ChildOperations;
                if (children.Any() && children.First().Type is IPointerTypeSymbol)
                {
                    // Pointer access. Probably.
                    // I mean, some operation with a pointer produced a non-pointer type.
                    return Problem.Ok;
                }

                // Otherwise we'll assume this is one of a plethora of other operations that might return a temporary.
                // f.e.: IDefaultValueOperation, IBinaryOperation, ...
                return Problem.Temporary;
            }
        }
    }

    private static Problem FindSymbolProblem(ISymbol symbol)
    {
        return symbol switch
        {
            IMethodSymbol method => FindMethodProblem(method),
            IPropertySymbol property => FindPropertyProblem(property),
            _ => Problem.Ok,
        };
    }

    private static Problem FindMethodProblem(IMethodSymbol method)
    {
        // This shouldn't happen, so we bail.
        if (method.ReturnsVoid) return Problem.Ok;

        // If the return type is not a value-type, any call is fine.
        if (!IsProblematicType(method.ReturnType)) return Problem.Ok;

        RefKind refKind = method.RefKind;
        if (refKind is RefKind.Ref) return Problem.Ok;
        if (refKind is RefKind.RefReadOnly) return Problem.ReadOnlyRef;

        return Problem.MethodReturn;
    }

    private static Problem FindPropertyProblem(IPropertySymbol property)
    {
        // If the return type is not a value-type, any call is fine.
        if (!IsProblematicType(property.Type)) return Problem.Ok;

        RefKind refKind = property.RefKind;
        if (refKind is RefKind.Ref) return Problem.Ok;
        if (refKind is RefKind.RefReadOnly) return Problem.ReadOnlyRef;

        return Problem.PropertyReturn;
    }

    private static bool IsProblematicType(ITypeSymbol type)
    {
        return !type.IsReadOnly
            && (type.IsValueType || type.TypeKind is TypeKind.TypeParameter && !type.IsReferenceType);
    }

    private static IOperation? FindInstance(IConditionalAccessInstanceOperation instance)
    {
        var parent = instance.Parent;
        while (parent != null)
        {
            if (parent is IConditionalAccessOperation access)
                return access.Operation;

            parent = parent.Parent;
        }

        return null;
    }

    private static IOperation? FindPatternInstance(IInstanceReferenceOperation instance)
    {
        var parent = instance.Parent;
        while (parent != null)
        {
            if (parent is IIsPatternOperation pattern)
                return pattern.Value;

            parent = parent.Parent;
        }

        return null;
    }

    private static bool IsThis(IOperation? operation)
    {
        return operation is IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance };
    }

    private static bool SkipReceiver(IOperation operation)
    {
        // Skip receivers if they refer to the instance (`this`, the receiver of `with` or an object initializer, etc)
        // or their static type has no chance to be a mutable struct.

        return operation is IInstanceReferenceOperation { ReferenceKind: not InstanceReferenceKind.PatternInput }
            || operation.Type?.TypeKind is not (TypeKind.Struct or TypeKind.TypeParameter);
    }

    private static ITypeSymbol ResolveReceiver(ITypeSymbol? actual, ITypeSymbol declared)
    {
        // Explicitly prefer a class that a method is declared on than the actual type, if the actual type is a generic parameter.
        // This can only refer to methods declared on Object, ValueType, and Enum.
        // Their virtual methods *could* mutate the receiver, but that leads to many false positives.

        return actual is null || actual.TypeKind is TypeKind.TypeParameter && declared.TypeKind is TypeKind.Class
            ? declared
            : actual;
    }

    private static SyntaxNode GetReportNode(IOperation receiver)
    {
        if (receiver is IConditionalAccessInstanceOperation { Parent: IOperation parent })
        {
            // Not happy with relying on C# syntax nodes here.
            // But VS refuses to display the warning correctly (plus I get to adjust the target to just the method name).
            var syntax = parent.Syntax;
            return syntax is InvocationExpressionSyntax invoke ? invoke.Expression : syntax;
        }

        return receiver.Syntax;
    }

    private static string TruncateAtNewLine(string text)
    {
        int index = text.AsSpan().IndexOfAny('\n', '\r');
        return (uint)index >= (uint)text.Length ? text : text[..index];
    }

    private static IMethodSymbol? AsMethod(ISymbol symbol)
    {
        return symbol switch
        {
            IMethodSymbol m => m,
            IPropertySymbol p => p.GetMethod,
            _ => null
        };
    }

    private enum Problem
    {
        Ok,
        ReadOnlyField,
        ReadOnlyRef,
        MethodReturn,
        PropertyReturn,
        MutableOverConditionalNullable,
        Temporary,
        ForEach,
        ImplicitClonedParam,
        Unknown
    }
}
