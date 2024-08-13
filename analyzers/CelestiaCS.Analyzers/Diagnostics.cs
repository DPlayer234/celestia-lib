using Microsoft.CodeAnalysis;

namespace CelestiaCS.Analyzers;

internal static class Diagnostics
{
    public static DiagnosticDescriptor Test { get; } = new(
        id: "CL9999",
        title: "Test",
        messageFormat: "Checked this: '{0}' with '{1}'",
        category: "Test",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    internal static class Performance
    {
        public const string Category = "CelestiaCS.Performance";

        public static DiagnosticDescriptor ImplicitBoxing { get; } = new(
            id: "CL9000",
            title: "Avoid implicit boxing",
            messageFormat: "Type '{0}' is boxed due to implicit conversion to '{1}'.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: false);
    }

    internal static class Correctness
    {
        public const string Category = "CelestiaCS.Correctness";

        public static DiagnosticDescriptor MutableOverReadOnlyField { get; } = new(
            id: "CL0001",
            title: "Mutable call on readonly field",
            messageFormat: "'{0}' is a readonly field. This call to '{1}' uses a cloned value.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MutableOverReadOnlyRef { get; } = new(
            id: "CL0002",
            title: "Mutable call on readonly ref",
            messageFormat: "'{0}' is a readonly ref. This call to '{1}' uses a cloned value.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MutableOverMethodReturn { get; } = new(
            id: "CL0003",
            title: "Mutable call on method return value",
            messageFormat: "The return value of '{0}' is not a variable, but '{1}' may try to mutate it.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MutableOverPropertyReturn { get; } = new(
            id: "CL0004",
            title: "Mutable call on property value",
            messageFormat: "The return value of '{0}' is not a variable, but '{1}' may try to mutate it.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MutableOverConditionalNullable { get; } = new(
            id: "CL0005",
            title: "Mutable call on conditional Nullable<T>",
            messageFormat: "The conditional access to '{0}' clones the value. This call to '{1}' cannot modify the original value.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MutableOverTemporary { get; } = new(
            id: "CL0006",
            title: "Mutable call on temporary",
            messageFormat: "'{0}' creates a temporary value, but '{1}' may try to mutate it.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MutableOverForEach { get; } = new(
            id: "CL0007",
            title: "Mutable call on foreach iteration variable",
            messageFormat: "'{0}' is a foreach iteration variable. This call to '{1}' may mutate the local value but does not modify the collection.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MutableOverImplicitClonedParam { get; } = new(
            id: "CL0010",
            title: "Mutable call on cloned parameter",
            messageFormat: "'{0}' is a local cloned value. Changes made by '{1}' may not surface to the caller.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: false);

        public static DiagnosticDescriptor MutableOverUnknown { get; } = new(
            id: "CL0099",
            title: "Mutable call on unknown pattern",
            messageFormat: "'{0}' might be read-only and the call to '{1}' may try to mutate it.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    }

    internal static class ILEmit
    {
        public const string Category = "CelestiaCS.ILEmit";

        public static DiagnosticDescriptor IncorrectILEmitArgType { get; } = new(
            id: "CL1001",
            title: "Provide correct OpCode argument",
            messageFormat: "OpCode '{0}' expects an argument of type {1}.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor NoILEmitArg { get; } = new(
            id: "CL1002",
            title: "Provide no OpCode argument",
            messageFormat: "OpCode '{0}' expects no inline arguments.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor UseEmitCalliILEmit { get; } = new(
            id: "CL1003",
            title: "Use EmitCalli for this OpCode",
            messageFormat: "OpCode '{0}' should be used with EmitCalli.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    }
}
