namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class InterpolatedStringHandlerArgumentAttribute : Attribute
{
    public InterpolatedStringHandlerArgumentAttribute(string argument) => Arguments = [argument];
    public InterpolatedStringHandlerArgumentAttribute(params string[] arguments) => Arguments = arguments;

    public string[] Arguments { get; }
}
