using System;

namespace CelestiaCS.Lib.Commands.Attributes;

/// <summary>
/// Declares a method or class as a command.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class CommandAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandAttribute"/> class with the specified name.
    /// </summary>
    /// <param name="name"> The name of the command. </param>
    public CommandAttribute(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }

    /// <summary> The name of the command. </summary>
    public string Name { get; }
}
