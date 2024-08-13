using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CelestiaCS.Lib.Reflection;

/// <summary>
/// Provides some static methods and properties for use with IL-emit.
/// </summary>
public static class IL
{
    /// <summary> Flags to get any instance member. </summary>
    public const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    /// <summary> Flags to get any static member. </summary>
    public const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    /// <summary> Flags to get any member. </summary>
    public const BindingFlags AnyFlags = InstanceFlags | StaticFlags;

    /// <summary>
    /// Creates a delegate that calls the provided constructor and returns the created object.
    /// </summary>
    /// <typeparam name="TDelegate"> The delegate type to create. </typeparam>
    /// <param name="ctor"> The constructor to call. </param>
    /// <returns> The compiled delegate. </returns>
    public static TDelegate CreateDelegate<TDelegate>(this ConstructorInfo ctor)
        where TDelegate : Delegate
    {
        Debug.Assert(ctor.DeclaringType != null);
        var (returnType, parameterTypes) = ExtractDelegateSignature<TDelegate>();

        var method = new DynamicMethod
        (
            name: ctor.DeclaringType.Name,
            attributes: MethodAttributes.Public | MethodAttributes.Static,
            callingConvention: CallingConventions.Standard,
            returnType,
            parameterTypes: [typeof(object), .. parameterTypes],
            m: ctor.Module,
            skipVisibility: false
        );

        var il = method.GetILGenerator();

        for (int i = 0; i < parameterTypes.Length; i++)
        {
            // Skip dummy 0th argument
            il.Emit(OpCodes.Ldarg, i + 1);
        }

        il.Emit(OpCodes.Newobj, ctor);
        il.Emit(OpCodes.Ret);

        return method.CreateDelegate<TDelegate>(ctor.DeclaringType);
    }

    #region Throwing Reflection Wrappers

    /// <summary> Gets a field. </summary>
    /// <param name="type"> The type to reflect. </param>
    /// <param name="fieldName"> The field to look for. </param>
    /// <param name="flags"> The binding flags. </param>
    /// <returns> The found field. </returns>
    /// <exception cref="ILMissingMemberException"> The member was not found. </exception>
    public static FieldInfo GetField(Type type, string fieldName, BindingFlags flags)
    {
        return type.GetField(fieldName, flags)
            ?? FieldNotFound(type, fieldName);
    }

    /// <summary> Gets a method with any parameters. </summary>
    /// <param name="type"> The type to reflect. </param>
    /// <param name="methodName"> The method to look for. </param>
    /// <param name="flags"> The binding flags. </param>
    /// <returns> The found method. </returns>
    /// <exception cref="ILMissingMemberException"> The member was not found. </exception>
    /// <exception cref="AmbiguousMatchException"> The member was not uniquely identifiable. </exception>
    public static MethodInfo GetMethod(Type type, string methodName, BindingFlags flags)
    {
        return type.GetMethod(methodName, flags)
            ?? MethodNotFound(type, methodName);
    }

    /// <summary> Gets a method with the provided argument types. </summary>
    /// <param name="type"> The type to reflect. </param>
    /// <param name="methodName"> The method to look for. </param>
    /// <param name="flags"> The binding flags. </param>
    /// <param name="arguments"> The argument types. </param>
    /// <returns> The found method. </returns>
    /// <exception cref="ILMissingMemberException"> The member was not found. </exception>
    public static MethodInfo GetMethod(Type type, string methodName, BindingFlags flags, Type[] arguments)
    {
        return type.GetMethod(methodName, flags, null, arguments, null)
            ?? MethodNotFound(type, methodName);
    }

    /// <summary> Gets a constructor with the provided argument types. </summary>
    /// <param name="type"> The type to reflect. </param>
    /// <param name="arguments"> The argument types. </param>
    /// <returns> The found constructor. </returns>
    /// <exception cref="ILMissingMemberException"> The member was not found. </exception>
    public static ConstructorInfo GetConstructor(Type type, Type[] arguments)
    {
        return type.GetConstructor(InstanceFlags, null, arguments, null)
            ?? ConstructorNotFound(type);
    }

    /// <summary> Gets a property. </summary>
    /// <param name="type"> The type to reflect. </param>
    /// <param name="propertyName"> The property to look for. </param>
    /// <param name="flags"> The binding flags. </param>
    /// <param name="arguments"> The argument types. </param>
    /// <returns> The found property. </returns>
    /// <exception cref="ILMissingMemberException"> The member was not found. </exception>
    public static PropertyInfo GetProperty(Type type, string propertyName, BindingFlags flags, Type[]? arguments = null)
    {
        return type.GetProperty(propertyName, flags, null, null, arguments ?? Type.EmptyTypes, null)
            ?? PropertyNotFound(type, propertyName);
    }

    /// <summary> Gets an event. </summary>
    /// <param name="type"> The type to reflect. </param>
    /// <param name="eventName"> The event to look for. </param>
    /// <param name="flags"> The binding flags. </param>
    /// <returns> The found event. </returns>
    /// <exception cref="ILMissingMemberException"> The member was not found. </exception>
    public static EventInfo GetEvent(Type type, string eventName, BindingFlags flags)
    {
        return type.GetEvent(eventName, flags)
            ?? EventNotFound(type, eventName);
    }

    /// <summary> Gets the getter for a property. </summary>
    /// <param name="type"> The type to reflect. </param>
    /// <param name="propertyName"> The property to look for. </param>
    /// <param name="flags"> The binding flags. </param>
    /// <param name="arguments"> The argument types. </param>
    /// <returns> The found property getter. </returns>
    /// <exception cref="ILMissingMemberException"> The member was not found. </exception>
    public static MethodInfo GetPropertyGetter(Type type, string propertyName, BindingFlags flags, Type[]? arguments = null)
    {
        return GetPropertyGetter(type, GetProperty(type, propertyName, flags, arguments));
    }

    /// <summary> Gets the setter for a property. </summary>
    /// <param name="type"> The type to reflect. </param>
    /// <param name="propertyName"> The property to look for. </param>
    /// <param name="flags"> The binding flags. </param>
    /// <param name="arguments"> The argument types. </param>
    /// <returns> The found property setter. </returns>
    /// <exception cref="ILMissingMemberException"> The member was not found. </exception>
    public static MethodInfo GetPropertySetter(Type type, string propertyName, BindingFlags flags, Type[]? arguments = null)
    {
        return GetPropertySetter(type, GetProperty(type, propertyName, flags, arguments));
    }

    private static MethodInfo GetPropertyGetter(Type type, PropertyInfo property)
    {
        return property.GetMethod ?? MethodNotFound(type, $"get_{property.Name}");
    }

    private static MethodInfo GetPropertySetter(Type type, PropertyInfo property)
    {
        return property.SetMethod ?? MethodNotFound(type, $"set_{property.Name}");
    }

    #endregion

    #region Throw Helpers

    private static FieldInfo FieldNotFound(Type type, string fieldName)
    {
        throw new ILMissingMemberException(type, fieldName, ILMissingMemberType.Field);
    }

    private static MethodInfo MethodNotFound(Type type, string methodName)
    {
        throw new ILMissingMemberException(type, methodName, ILMissingMemberType.Method);
    }

    private static ConstructorInfo ConstructorNotFound(Type type)
    {
        throw new ILMissingMemberException(type, ".ctor", ILMissingMemberType.Constructor);
    }

    private static PropertyInfo PropertyNotFound(Type type, string propertyName)
    {
        throw new ILMissingMemberException(type, propertyName, ILMissingMemberType.Property);
    }

    private static EventInfo EventNotFound(Type type, string eventName)
    {
        throw new ILMissingMemberException(type, eventName, ILMissingMemberType.Event);
    }

    #endregion

    #region Extracting Delegate Signatures

    private static (Type returnType, Type[] argumentTypes) ExtractDelegateSignature<TDelegate>()
        where TDelegate : Delegate
    {
        var invokeMethod = typeof(TDelegate).GetMethod("Invoke") ?? throw new ArgumentException("The given type is not a specific delegate type.");
        return (invokeMethod.ReturnType, invokeMethod.GetParameters().Select(p => p.ParameterType).ToArray());
    }

    #endregion
}
