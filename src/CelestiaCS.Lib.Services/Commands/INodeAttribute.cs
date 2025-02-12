namespace CelestiaCS.Lib.Commands;

/// <summary>
/// Represents an attribute for a command, group, or parameter node.
/// </summary>
public interface INodeAttribute
{
    /// <summary>
    /// Gets whether this attribute spreads to child nodes on build.
    /// </summary>
    /// <remarks>
    /// Regardless of the value, attributes will not spread to <see cref="ParameterNode"/>s.
    /// </remarks>
    bool SpreadsToChildren => false;
}
