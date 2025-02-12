using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using CelestiaCS.Lib.Linq;

namespace CelestiaCS.Lib.Commands;

/// <summary>
/// Represents a group of commands and additional groups in a command tree.
/// </summary>
/// <remarks>
/// The root of the tree is also represented with this type, usually with the name of <see cref="string.Empty"/>.
/// </remarks>
[DebuggerDisplay("Group: Name = {Name}")]
public sealed record GroupNode : CommandOrGroupNode
{
    /// <summary> Gets the immediate child nodes of this group. </summary>
    public required FrozenDictionary<string, CommandOrGroupNode> Children { get; init; }

    /// <inheritdoc/>
    public override Builder ToBuilder()
    {
        var builder = new Builder();
        FillBuilder(builder);

        builder.Children.AddRange(Children.Values.Select(v => v.ToBuilder()));

        // Trim away spread-attributes from the immediate child nodes.
        // When the builder for the child node was created, it will already have done so for its children.
        foreach (var attr in builder.Attributes)
        {
            if (!attr.SpreadsToChildren) continue;

            foreach (var child in builder.Children)
                RemoveByRef(child.Attributes, attr);
        }

        return builder;

        static void RemoveByRef(List<INodeAttribute> attributes, INodeAttribute attribute)
        {
            for (int i = attributes.Count - 1; i >= 0; i--)
            {
                if (attributes[i] == attribute)
                {
                    attributes.RemoveAt(i);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Enumerates all children below this node.
    /// </summary>
    /// <returns> An enumerator over the child nodes. </returns>
    public IEnumerable<CommandOrGroupNode> EnumerateAllChildren()
    {
        var values = Children.Values;
        if (values.IsEmpty) return [];

        var raw = ImmutableCollectionsMarshal.AsArray(values)!;
        return raw.Concat(raw.OfType<GroupNode>().SelectMany(v => v.EnumerateAllChildren()));
    }

    /// <summary>
    /// Combines the nodes into one.
    /// </summary>
    /// <param name="nodes"> The nodes. </param>
    /// <returns> The combined group node. </returns>
    public static GroupNode Combine(IEnumerable<GroupNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        var imm = nodes.Where(n => n.Children.Count != 0).ToImmutableArray();
        if (imm.Length == 0) return nodes.First();
        if (imm.Length == 1) return imm[0];

        var attr = imm.Select(n => n.Attributes).Except(NodeAttributeCollection.Empty).ToImmutableArray();

        return imm[0] with
        {
            Children = imm.SelectMany(n => n.Children.Values)
                .GroupBy(n => n.Name)
                .Select(NestedCombine)
                .ToFrozenDictionary(n => n.Name),
            Attributes = attr.Length switch
            {
                0 => NodeAttributeCollection.Empty,
                1 => attr[0],
                _ => NodeAttributeCollection.Create(ImmutableCollectionsMarshal.AsImmutableArray(attr.ConcatToArray(a => a)))
            }
        };
    }

    private static CommandOrGroupNode NestedCombine(IEnumerable<CommandOrGroupNode> nodes)
    {
        // Elements of GroupBy should be safe to iterate multiple times.
        // Currently, the groupings are implemented with IList<T>.
        if (nodes.TryAsSingle(out var n))
            return n;

        if (nodes.Any(n => n is not GroupNode))
            ThrowHelper.Argument(nameof(nodes), $"Cannot combine nodes with name '{nodes.First().Name}' as it includes non-group nodes.");

        return Combine(nodes.Cast<GroupNode>());
    }

    /// <summary>
    /// Provides a way to incrementally build a group node.
    /// </summary>
    [DebuggerDisplay("Group: Name = {Name}")]
    public new sealed class Builder : CommandOrGroupNode.Builder
    {
        /// <inheritdoc cref="GroupNode.Children"/>
        public List<CommandOrGroupNode.Builder> Children { get; } = [];

        /// <summary>
        /// Adds a child node to this group.
        /// </summary>
        /// <param name="child"> The child node to add. </param>
        /// <returns> The same builder. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="child"/> is null. </exception>
        public Builder AddNode(CommandOrGroupNode.Builder child)
        {
            ArgumentNullException.ThrowIfNull(child);

            Children.Add(child);
            return this;
        }

        /// <inheritdoc/>
        public override GroupNode Build()
        {
            return Build(ImmutableArray<INodeAttribute>.Empty);
        }

        internal override GroupNode Build(ImmutableArray<INodeAttribute> attributes)
        {
            ArgumentNullException.ThrowIfNull(Name);

            if (HasDuplicates(out string? dup))
            {
                ThrowHelper.Argument(nameof(Children), $"Duplicated child name '{dup}', within group '{Name}'.");
            }

            var nextAttributes = attributes.AddRange(Attributes.Where(a => a.SpreadsToChildren));
            return new GroupNode
            {
                Name = Name,
                Children = Children.Select(n => n.Build(nextAttributes)).ToFrozenDictionary(n => n.Name),
                Attributes = NodeAttributeCollection.Create(attributes.InsertRange(0, Attributes)),
            };
        }

        private bool HasDuplicates(out string? name)
        {
            HashSet<string?> seenNames = new();
            foreach (var n in Children)
            {
                if (!seenNames.Add(n.Name))
                {
                    name = n.Name;
                    return true;
                }
            }

            name = null;
            return false;
        }
    }
}
