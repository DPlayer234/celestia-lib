using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CelestiaCS.Lib.Commands.Attributes;
using CelestiaCS.Lib.Linq;

namespace CelestiaCS.Lib.Commands;

/// <summary>
/// Provides a base type to allow building command trees from types.
/// </summary>
public abstract class TypeGroupBuilder
{
    private const BindingFlags MethodFlags = BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static;
    private const BindingFlags GroupFlags = BindingFlags.Public | BindingFlags.DeclaredOnly;

    private const string RootName = "";

    /// <summary>
    /// Builds an assembly root group by scanning the assembly for public types attributed with <see cref="CommandAttribute"/> or <see cref="GroupAttribute"/>.
    /// </summary>
    /// <param name="assembly"> The assembly to scan. </param>
    /// <returns> The root group. </returns>
    public GroupNode BuildAssemblyRoot(Assembly assembly)
    {
        var builder = new GroupNode.Builder().WithName(RootName);
        ScanAndAppend(builder, assembly.ExportedTypes.Where(e => !e.IsNested));
        return builder.Build();
    }

    /// <summary>
    /// Builds an type root group by scanning for public methods and nested types attributed with <see cref="CommandAttribute"/> or <see cref="GroupAttribute"/>.
    /// </summary>
    /// <param name="type"> The type to scan. </param>
    /// <returns> The root group. </returns>
    public GroupNode BuildTypeRoot(Type type)
    {
        return CreateGroupBuilder(type, RootName).Build();
    }

    /// <summary>
    /// Builds a group from a type by scanning for public methods and nested types attributed with <see cref="CommandAttribute"/> or <see cref="GroupAttribute"/>.
    /// </summary>
    /// <param name="type"> The type to scan. </param>
    /// <param name="name"> The name to set. </param>
    /// <returns> The type group. </returns>
    public GroupNode.Builder CreateGroupBuilder(Type type, string name)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(name);

        var builder = new GroupNode.Builder()
            .WithName(name)
            .AddAttributes(type.GetCustomAttributes().OfType<INodeAttribute>());

        foreach (var m in type.GetMembers(MethodFlags))
        {
            if (m is Type) continue;

            var commandAttr = m.GetCustomAttribute<CommandAttribute>();
            if (commandAttr != null)
            {
                builder.AddNode(CreateCommandBuilder(m, commandAttr.Name));
                continue;
            }
        }

        ScanAndAppend(builder, type.GetNestedTypes(GroupFlags));

        ExtendGroup(new GroupContext
        (
            builder,
            type
        ));

        return builder;
    }

    /// <summary>
    /// Builds a command from a member. Attributes are taken from the <paramref name="member"/> directly.
    /// </summary>
    /// <remarks>
    /// If used with a <see cref="MethodInfo"/>, parses it directly. If used with a <see cref="Type"/>, looks for a public <c>InvokeAsync</c> method.
    /// </remarks>
    /// <param name="member"> The member to scan. </param>
    /// <param name="name"> The name to set. </param>
    /// <returns> The member command. </returns>
    public CommandNode.Builder CreateCommandBuilder(MemberInfo member, string name)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(name);

        var builder = new CommandNode.Builder()
            .WithName(name)
            .AddAttributes(member.GetCustomAttributes().OfType<INodeAttribute>());

        var methodInfo = member as MethodInfo;
        if (methodInfo == null)
        {
            if (member is Type t)
            {
                ValidateNoNestedGroupsOrCommands(t);

                methodInfo = t.GetMethod("InvokeAsync", MethodFlags);
                if (methodInfo == null)
                    ThrowHelper.ArgumentCombined($"Command type '{t.FullName}' has no InvokeAsync method.");

                if (methodInfo.GetCustomAttributes().AnyOfType<INodeAttribute>())
                    ThrowHelper.ArgumentCombined($"Command type '{t.FullName}' specifies {nameof(INodeAttribute)}s on the command method, which is invalid.");
            }
            else
            {
                ThrowHelper.ArgumentCombined($"'{member}' cannot be a command.");
            }
        }

        AddParameters(builder, methodInfo);

        ExtendCommand(new CommandContext
        (
            builder,
            member,
            methodInfo
        ));

        return builder;
    }

    /// <summary>
    /// Extends the properties for this a <see cref="GroupNode"/>.
    /// </summary>
    /// <param name="context"> The call context. </param>
    protected abstract void ExtendGroup(GroupContext context);

    /// <summary>
    /// Extends the properties for this a <see cref="CommandNode"/>.
    /// </summary>
    /// <remarks>
    /// This has to set an invocation delegate.
    /// </remarks>
    /// <param name="context"> The call context. </param>
    protected abstract void ExtendCommand(CommandContext context);

    /// <summary>
    /// Extends the properties for this a <see cref="ParameterNode"/>.
    /// </summary>
    /// <param name="context"> The call context. </param>
    protected abstract void ExtendParameter(ParameterContext context);

    private void ScanAndAppend(GroupNode.Builder builder, IEnumerable<Type> types)
    {
        foreach (var t in types)
        {
            var groupAttr = t.GetCustomAttribute<GroupAttribute>();
            var commandAttr = t.GetCustomAttribute<CommandAttribute>();

            if (groupAttr != null)
            {
                if (commandAttr != null)
                    ThrowHelper.ArgumentCombined($"'{t.FullName}' is attributed as both a group and a command. This is not allowed.");

                builder.AddNode(CreateGroupBuilder(t, groupAttr.Name));
                continue;
            }

            if (commandAttr != null)
            {
                builder.AddNode(CreateCommandBuilder(t, commandAttr.Name));
                continue;
            }
        }
    }

    private void ValidateNoNestedGroupsOrCommands(Type type)
    {
        foreach (var m in type.GetMembers(MethodFlags))
        {
            if (m.IsDefined(typeof(CommandAttribute)) || m.IsDefined(typeof(GroupAttribute)))
            {
                ThrowHelper.ArgumentCombined($"Command type '{type}' cannot nest further commands or groups, like '{m}'.");
            }
        }
    }

    private void AddParameters(CommandNode.Builder command, MethodInfo methodInfo)
    {
        foreach (var param in methodInfo.GetParameters())
        {
            var paramType = param.ParameterType;

            // Unwrap parameter type
            var nonNullParamType = Nullable.GetUnderlyingType(paramType);
            if (nonNullParamType != null)
                paramType = nonNullParamType;

            var builder = new ParameterNode.Builder()
                .WithName(param.Name ?? "-")
                .WithType(paramType)
                .AddAttributes(param.GetCustomAttributes().OfType<INodeAttribute>());

            ExtendParameter(new ParameterContext(command, builder, methodInfo, param));
            command.AddParameter(builder);
        }
    }

    /// <summary> Parameters to <see cref="ExtendGroup(GroupContext)"/>. </summary>
    /// <param name="Group"> The group to be extended. </param>
    /// <param name="SourceType"> The source type that this group was built from. </param>
    protected readonly record struct GroupContext
    (
        GroupNode.Builder Group,
        Type SourceType
    );

    /// <summary> Parameters to <see cref="ExtendCommand(CommandContext)"/>. </summary>
    /// <param name="Command"> The command to be extended. </param>
    /// <param name="SourceMember"> The source member that this command was built from. </param>
    /// <param name="SourceMethod"> The source method that this command was built from. This may be equal to <see cref="SourceMember"/>. </param>
    protected readonly record struct CommandContext
    (
        CommandNode.Builder Command,
        MemberInfo SourceMember,
        MethodInfo SourceMethod
    );

    /// <summary> Parameters to <see cref="ExtendParameter(ParameterContext)"/>. </summary>
    /// <param name="Command"> The command that this parameter belongs to. </param>
    /// <param name="Parameter"> The parameter to be extended. </param>
    /// <param name="SourceMethod"> The source method that this parameter was for. </param>
    /// <param name="SourceParameter"> The source parameter that this parameter was built from. </param>
    protected readonly record struct ParameterContext
    (
        CommandNode.Builder Command,
        ParameterNode.Builder Parameter,
        MethodInfo SourceMethod,
        ParameterInfo SourceParameter
    );
}
