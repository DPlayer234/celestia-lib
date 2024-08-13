using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace CelestiaCS.Lib.Json;

/// <summary>
/// Provides modifiers to use with <see cref="DefaultJsonTypeInfoResolver"/> or <see cref="JsonTypeInfoResolver.WithAddedModifier"/>.
/// </summary>
public static class JsonTypeInfoModifiers
{
    /// <summary> Makes all properties required if the type has no required properties already. </summary>
    /// <remarks> This is only applicable to types without a custom converter. </remarks>
    /// <param name="typeInfo"> The type info to modify. </param>
    public static void MakeAllPropertiesRequired(JsonTypeInfo typeInfo)
    {
        // Only applicable to Object types.
        // Also skip types that already specify required properties.
        if (typeInfo.Kind != JsonTypeInfoKind.Object || typeInfo.Properties.Any(p => p.IsRequired))
            return;

        foreach (var p in typeInfo.Properties)
        {
            // Ignore the extension data.
            if (p.IsExtensionData || p.Set == null)
                continue;

            p.IsRequired = true;
        }
    }

    /// <summary> Allows a missing property to be specified in JSON even when <see cref="JsonUnmappedMemberHandling.Disallow"/> is used. </summary>
    /// <typeparam name="T"> The type of the property. </typeparam>
    /// <param name="propertyName"> The name of the property. </param>
    /// <returns> A modifier that can be added to a resolver. </returns>
    public static Action<JsonTypeInfo> AllowProperty<T>(string propertyName) => typeInfo =>
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object || typeInfo.Properties.Any(p => p.Name == propertyName))
            return;

        var property = typeInfo.CreateJsonPropertyInfo(typeof(T), propertyName);

        object? defaultValue = default(T);
        property.Get = _ => defaultValue;
        property.ShouldSerialize = (_, _) => false;
        property.IsRequired = false;

        typeInfo.Properties.Add(property);
    };

    /// <summary> Gets the effective <see cref="IJsonTypeInfoResolver"/> for the options instance. </summary>
    /// <remarks> This will return the default resolver if unset. </remarks>
    /// <param name="options"> The options instance. </param>
    /// <returns> The effective resolver. </returns>
    public static IJsonTypeInfoResolver GetTypeInfoResolver(this JsonSerializerOptions options)
    {
        return options.TypeInfoResolver ?? JsonSerializerOptions.Default.TypeInfoResolver!;
    }
}
