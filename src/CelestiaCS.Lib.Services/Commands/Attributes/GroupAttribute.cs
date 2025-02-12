using System;

namespace CelestiaCS.Lib.Commands.Attributes;

/// <summary>
/// Specifies that a class should be treated as a group.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class GroupAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GroupAttribute"/> class with the specified name.
    /// </summary>
    /// <param name="name"> The name of the group. </param>
    public GroupAttribute(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }

    /// <summary> The name of the group. </summary>
    public string Name { get; }
}
