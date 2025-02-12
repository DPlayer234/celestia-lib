using System;
using System.Diagnostics;

namespace CelestiaCS.Lib.Commands;

/// <summary>
/// Represents a parameter to a <see cref="CommandNode"/>.
/// </summary>
[DebuggerDisplay("Parameter: Name = {Name}")]
public sealed record ParameterNode : GenericNode
{
    /// <summary> The type of the parameter. </summary>
    public required Type Type { get; init; }

    /// <inheritdoc/>
    public override Builder ToBuilder()
    {
        var builder = new Builder();
        FillBuilder(builder);

        builder.Type = Type;
        return builder;
    }

    /// <summary>
    /// Provides a way to incrementally build a parameter node.
    /// </summary>
    [DebuggerDisplay("Parameter: Name = {Name}")]
    public new sealed class Builder : GenericNode.Builder
    {
        /// <inheritdoc cref="ParameterNode.Type"/>
        public Type? Type { get; set; }

        /// <summary>
        /// Sets the type of the parameter.
        /// </summary>
        /// <param name="type"> The new type of the parameter. </param>
        /// <returns> The same builder. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="type"/> is null. </exception>
        public Builder WithType(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            Type = type;
            return this;
        }

        /// <inheritdoc/>
        public override ParameterNode Build()
        {
            ArgumentNullException.ThrowIfNull(Name);
            ArgumentNullException.ThrowIfNull(Type);

            return new ParameterNode
            {
                Name = Name,
                Type = Type,
                Attributes = NodeAttributeCollection.Create(Attributes)
            };
        }
    }
}
