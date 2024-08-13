using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace CelestiaCS.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ILGeneratorAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        Diagnostics.ILEmit.IncorrectILEmitArgType,
        Diagnostics.ILEmit.NoILEmitArg,
        Diagnostics.ILEmit.UseEmitCalliILEmit
    ];

    private readonly Dictionary<string, OpCode> _opCodes = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.FieldType == typeof(OpCode))
        .ToDictionary(f => f.Name, f => (OpCode)f.GetValue(null));

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterOperationAction(
            action: CheckInvocation,
            operationKinds: OperationKind.Invocation);
    }

    private void CheckInvocation(OperationAnalysisContext ctx)
    {
        var invocation = (IInvocationOperation)ctx.Operation;

        // Check for non-extension `Emit` call on an ILGenerator.
        var method = invocation.TargetMethod;
        if (method.Name != "Emit" || method.IsExtensionMethod) return;

        var ilGeneratorType = ctx.Compilation.GetTypeByMetadataName("System.Reflection.Emit.ILGenerator");
        if (!SymbolEqualityComparer.Default.Equals(method.ContainingType, ilGeneratorType)) return;

        // Validate parameters being allowed.
        var arguments = invocation.Arguments;
        var parameters = method.Parameters;

        if (parameters.IsEmpty) return;
        if (arguments.Length != parameters.Length) return;

        // Look for the OpCode parameter.
        // We only continue if the parameter is immediately an access on `OpCodes`.
        var opCodeValue = FindParameterValue(parameters[0], arguments);
        if (opCodeValue == null) return;
        if (opCodeValue is not IFieldReferenceOperation { Instance: null } opCodeFieldRef) return;

        var opCodeField = opCodeFieldRef.Field;
        var opCodesType = ctx.Compilation.GetTypeByMetadataName("System.Reflection.Emit.OpCodes");
        if (!SymbolEqualityComparer.Default.Equals(opCodeField.ContainingType, opCodesType)) return;

        // Grab the correct `OpCode` information.
        if (!_opCodes.TryGetValue(opCodeField.Name, out OpCode opCode)) return;

        // Grab the allowed inline arguments to that OpCode.
        // The OpCodes at the start are known to us to have slightly different behavior, so they're special cased.
        // This includes a combination of ILGenerator special code (i.e. down-casting Int32) and ones allowing constructors.
        var expectedArg = (ushort)opCode.Value switch
        {
            0xfe09 or 0xfe0a or 0xfe0b => OpCodeArg.Int16 | OpCodeArg.Int32, // ldarg, ldarga, starg
            0xfe0c or 0xfe0d or 0xfe0e => OpCodeArg.Int16 | OpCodeArg.Int32 | OpCodeArg.LocalBuilder, // ldloc, ldloca, stloc
            0x28 => OpCodeArg.MethodInfo | OpCodeArg.ConstructorInfo, // call
            0x73 => OpCodeArg.ConstructorInfo, // newobj
            _ => opCode.OperandType switch
            {
                OperandType.InlineBrTarget => OpCodeArg.Label,
                OperandType.InlineField => OpCodeArg.FieldInfo,
                OperandType.InlineI => OpCodeArg.Int32,
                OperandType.InlineI8 => OpCodeArg.Int64,
                OperandType.InlineMethod => OpCodeArg.MethodInfo,
                OperandType.InlineR => OpCodeArg.Double,
                OperandType.InlineSig => OpCodeArg.EmitCall,
                OperandType.InlineString => OpCodeArg.String,
                OperandType.InlineSwitch => OpCodeArg.LabelArray,
                OperandType.InlineTok => OpCodeArg.FieldInfo | OpCodeArg.MethodInfo | OpCodeArg.Type,
                OperandType.InlineType => OpCodeArg.Type,
                OperandType.InlineVar => OpCodeArg.Int16 | OpCodeArg.LocalBuilder,
                OperandType.ShortInlineBrTarget => OpCodeArg.Label,
                OperandType.ShortInlineI => OpCodeArg.Byte,
                OperandType.ShortInlineR => OpCodeArg.Single,
                OperandType.ShortInlineVar => OpCodeArg.Byte | OpCodeArg.LocalBuilder,
                OperandType.InlineNone => OpCodeArg.None,
                _ => OpCodeArg.Unknown
            }
        };

        if (expectedArg == OpCodeArg.None)
        {
            if (parameters.Length != 1)
                goto IsProblem;

            return;
        }

        if (parameters.Length != 2)
            goto IsProblem;

        var opCodeInlineParam = parameters[1];
        var actualArg = opCodeInlineParam.Type switch
        {
            { SpecialType: SpecialType.System_SByte or SpecialType.System_Byte } => OpCodeArg.Byte,
            { SpecialType: SpecialType.System_Int16 } => OpCodeArg.Int16,
            { SpecialType: SpecialType.System_Int32 } => OpCodeArg.Int32,
            { SpecialType: SpecialType.System_Int64 } => OpCodeArg.Int64,
            { SpecialType: SpecialType.System_Single } => OpCodeArg.Single,
            { SpecialType: SpecialType.System_Double } => OpCodeArg.Double,
            { SpecialType: SpecialType.System_String } => OpCodeArg.String,
            INamedTypeSymbol { Name: "Label" } => OpCodeArg.Label,
            IArrayTypeSymbol { ElementType.Name: "Label" } => OpCodeArg.LabelArray,
            INamedTypeSymbol { Name: "FieldInfo" } => OpCodeArg.FieldInfo,
            INamedTypeSymbol { Name: "MethodInfo" } => OpCodeArg.MethodInfo,
            INamedTypeSymbol { Name: "ConstructorInfo" } => OpCodeArg.ConstructorInfo,
            INamedTypeSymbol { Name: "Type" } => OpCodeArg.Type,
            INamedTypeSymbol { Name: "LocalBuilder" } => OpCodeArg.LocalBuilder,
            _ => OpCodeArg.Unknown
        };

        if (!expectedArg.HasFlag(actualArg))
            goto IsProblem;

        return;

    IsProblem:
        EmitDiagnostic(in ctx, opCodeValue, opCodeField.Name, expectedArg);
    }

    private static void EmitDiagnostic(in OperationAnalysisContext ctx, IOperation opCodeValue, string name, OpCodeArg expectedArg)
    {
        ctx.ReportDiagnostic(expectedArg switch
        {
            OpCodeArg.None => Diagnostic.Create(Diagnostics.ILEmit.NoILEmitArg, opCodeValue.Syntax.GetLocation(), name),
            OpCodeArg.EmitCall => Diagnostic.Create(Diagnostics.ILEmit.UseEmitCalliILEmit, opCodeValue.Syntax.GetLocation(), name),
            _ => Diagnostic.Create(Diagnostics.ILEmit.IncorrectILEmitArgType, opCodeValue.Syntax.GetLocation(), name, expectedArg.ToString().Replace(", ", "/")),
        });
    }

    private static IOperation? FindParameterValue(IParameterSymbol parameter, ImmutableArray<IArgumentOperation> arguments)
    {
        foreach (var argument in arguments)
        {
            if (SymbolEqualityComparer.Default.Equals(parameter, argument.Parameter))
                return argument.Value;
        }

        return null;
    }

    [Flags]
    private enum OpCodeArg : uint
    {
        None,
        Byte = 0x1, // byte/sbyte
        Int16 = 0x2, // short
        Int32 = 0x4, // int
        Int64 = 0x8, // long
        Single = 0x10, // float
        Double = 0x20, // double
        String = 0x40, // string
        Label = 0x80, // Label
        LabelArray = 0x100, // Label[]
        FieldInfo = 0x200, // FieldInfo
        MethodInfo = 0x400, // MethodInfo
        ConstructorInfo = 0x800, // ConstructorInfo
        Type = 0x1000, // Type
        LocalBuilder = 0x2000, // LocalBuilder
        EmitCall = 0x4000_0000, // Use EmitCall method instead
        Unknown = 0x8000_0000,
    }
}
