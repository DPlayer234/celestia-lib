using System;
using System.Collections.Generic;

namespace CelestiaCS.Lib.Commands;

/// <summary>
/// Provides a general base class for other tree nodes.
/// </summary>
/// <remarks>
/// Unless you're writing methods that can use any node, using this type directly isn't particularly useful.
/// </remarks>
public abstract record GenericNode
{
    private protected GenericNode() { }

    /// <summary> Gets the name of the node. </summary>
    public required string Name { get; init; }

    /// <summary> Gets the collection of applied attributes. </summary>
    public NodeAttributeCollection Attributes { get; init; } = NodeAttributeCollection.Empty;

    /// <summary>
    /// Creates a builder with the same content as this node.
    /// </summary>
    /// <returns> A builder that can reconstruct this node. </returns>
    public abstract Builder ToBuilder();

    private protected void FillBuilder(Builder builder)
    {
        builder.Name = Name;
        builder.Attributes.AddRange(Attributes);
    }

    /// <summary>
    /// Provides a way to incrementally build a node.
    /// </summary>
    public abstract class Builder
    {
        private protected Builder() { }

        /// <inheritdoc cref="GenericNode.Name"/>
        public string? Name { get; set; }

        /// <inheritdoc cref="GenericNode.Attributes"/>
        public List<INodeAttribute> Attributes { get; } = [];

        /// <summary>
        /// Builds an immutable node from the data in this builder.
        /// </summary>
        /// <returns> An immutable node. </returns>
        /// <exception cref="ArgumentNullException"> A required property wasn't set. </exception>
        public abstract GenericNode Build();
    }
}

/// <summary>
/// Provides extensions for <see cref="GenericNode.Builder"/>s.
/// </summary>
public static class NodeBuilderExtensions
{
    /// <summary>
    /// Sets the name for this node.
    /// </summary>
    /// <typeparam name="T"> The node builder type. </typeparam>
    /// <param name="builder"> The node builder. </param>
    /// <param name="name"> The new name of the node. </param>
    /// <returns> The same builder. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="builder"/> or <paramref name="name"/> is null. </exception>
    public static T WithName<T>(this T builder, string name) where T : GenericNode.Builder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        builder.Name = name;
        return builder;
    }

    /// <summary>
    /// Adds an attribute to the node.
    /// </summary>
    /// <typeparam name="T"> The node builder type. </typeparam>
    /// <param name="builder"> The node builder. </param>
    /// <param name="attribute"> The attribute to add. </param>
    /// <returns> The same builder. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="builder"/> or <paramref name="attribute"/> is null. </exception>
    public static T AddAttribute<T>(this T builder, INodeAttribute attribute) where T : GenericNode.Builder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(attribute);

        builder.Attributes.Add(attribute);
        return builder;
    }

    /// <summary>
    /// Adds attributes to the node.
    /// </summary>
    /// <typeparam name="T"> The node builder type. </typeparam>
    /// <param name="builder"> The node builder. </param>
    /// <param name="attributes"> The attributes to add. </param>
    /// <returns> The same builder. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="builder"/> or <paramref name="attributes"/> is null. </exception>
    public static T AddAttributes<T>(this T builder, IEnumerable<INodeAttribute> attributes) where T : GenericNode.Builder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(attributes);

        builder.Attributes.AddRange(attributes);
        return builder;
    }

    /// <summary>
    /// Replaces the first attribute of some type with another.
    /// </summary>
    /// <typeparam name="T"> The node builder type. </typeparam>
    /// <typeparam name="TAttribute"> The node type. Usually specified via the lambda parameter. </typeparam>
    /// <param name="builder"> The node builder. </param>
    /// <param name="replacer"> A delegate that constructs a new attribute to replace the old one. </param>
    /// <returns> The same builder. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="builder"/> or <paramref name="replacer"/> is null. </exception>
    /// <exception cref="ArgumentException"> No attribute of type <typeparamref name="TAttribute"/> was found. </exception>
    public static T ReplaceAttribute<T, TAttribute>(this T builder, Func<TAttribute, INodeAttribute> replacer) where T : GenericNode.Builder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(replacer);

        for (int i = 0; i < builder.Attributes.Count; i++)
        {
            var attr = builder.Attributes[i];
            if (attr is TAttribute node)
            {
                builder.Attributes[i] = replacer(node);
                return builder;
            }
        }

        ThrowHelper.ArgumentCombined("Node type not found.");
        return null!;
    }
}
