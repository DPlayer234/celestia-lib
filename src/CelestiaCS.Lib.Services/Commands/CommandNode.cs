using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace CelestiaCS.Lib.Commands;

/// <summary>
/// Represents an invokable command in a command tree.
/// </summary>
[DebuggerDisplay("Command: Name = {Name}")]
public sealed record CommandNode : CommandOrGroupNode
{
    /// <summary> A delegate that allows invoking the command. </summary>
    public required Delegate Invoke { get; init; }

    /// <summary> Gets the information about the parameters. </summary>
    public required ImmutableArray<ParameterNode> Parameters { get; init; }

    /// <inheritdoc/>
    public override Builder ToBuilder()
    {
        var builder = new Builder();
        FillBuilder(builder);

        builder.Invoke = Invoke;
        builder.Parameters.AddRange(Parameters.Select(v => v.ToBuilder()));

        return builder;
    }

    /// <summary>
    /// Provides a way to incrementally build a command node.
    /// </summary>
    [DebuggerDisplay("Command: Name = {Name}")]
    public new sealed class Builder : CommandOrGroupNode.Builder
    {
        /// <inheritdoc cref="CommandNode.Invoke"/>
        public Delegate? Invoke { get; set; }

        /// <inheritdoc cref="CommandNode.Parameters"/>
        public List<ParameterNode.Builder> Parameters { get; } = [];

        /// <summary>
        /// Sets the invocation delegate for this command.
        /// </summary>
        /// <param name="invoke"> The new invocation delegate. </param>
        /// <returns> The same builder. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="invoke"/> is null. </exception>
        public Builder WithInvoke(Delegate invoke)
        {
            ArgumentNullException.ThrowIfNull(invoke);

            Invoke = invoke;
            return this;
        }

        /// <summary>
        /// Adds a parameter to this command.
        /// </summary>
        /// <param name="parameter"> The parameter to add. </param>
        /// <returns> The same builder. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="parameter"/> is null. </exception>
        public Builder AddParameter(ParameterNode.Builder parameter)
        {
            ArgumentNullException.ThrowIfNull(parameter);

            Parameters.Add(parameter);
            return this;
        }

        /// <inheritdoc/>
        public override CommandNode Build()
        {
            return Build(ImmutableArray<INodeAttribute>.Empty);
        }

        internal override CommandNode Build(ImmutableArray<INodeAttribute> attributes)
        {
            ArgumentNullException.ThrowIfNull(Name);
            ArgumentNullException.ThrowIfNull(Invoke);

            return new CommandNode
            {
                Name = Name,
                Invoke = Invoke,
                Parameters = Parameters.Select(p => p.Build()).ToImmutableArray(),
                Attributes = NodeAttributeCollection.Create(attributes.InsertRange(0, Attributes)),
            };
        }
    }
}
