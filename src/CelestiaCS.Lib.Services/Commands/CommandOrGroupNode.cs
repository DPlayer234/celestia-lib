using System.Collections.Immutable;

namespace CelestiaCS.Lib.Commands;

/// <summary>
/// Represents a command or group node in a command tree.
/// </summary>
public abstract record CommandOrGroupNode : GenericNode
{
    private protected CommandOrGroupNode() { }

    /// <inheritdoc/>
    public abstract override Builder ToBuilder();

    /// <summary>
    /// Provides a way to incrementally build a node.
    /// </summary>
    public new abstract class Builder : GenericNode.Builder
    {
        private protected Builder() { }

        /// <inheritdoc/>
        public override CommandOrGroupNode Build()
        {
            return Build(ImmutableArray<INodeAttribute>.Empty);
        }

        internal abstract CommandOrGroupNode Build(ImmutableArray<INodeAttribute> attributes);
    }
}
