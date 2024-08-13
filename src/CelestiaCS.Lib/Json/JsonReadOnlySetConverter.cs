using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CelestiaCS.Lib.Json;

// So STJ doesn't support IReadOnlySet<T> out-of-the-box

/// <summary>
/// Converts a <see cref="IReadOnlySet{T}"/> to or from JSON.
/// </summary>
/// <typeparam name="T"> The type of the collection values. </typeparam>
public sealed class JsonReadOnlySetConverter<T> : JsonConverter<IReadOnlySet<T>>
{
    public override IReadOnlySet<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<HashSet<T>>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlySet<T> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize<IEnumerable<T>>(value, options);
    }
}

/// <summary>
/// Produces converters that can convert any <see cref="IReadOnlySet{T}"/> to or from JSON.
/// </summary>
public sealed class JsonReadOnlySetConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert)
    {
        return GetCollectionType(typeToConvert) != null;
    }

    /// <inheritdoc/>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var t = GetCollectionType(typeToConvert);
        if (t is null) ThrowHelper.Argument(nameof(typeToConvert), "Type cannot be converted.");

        return (JsonConverter)Activator.CreateInstance(typeof(JsonReadOnlySetConverter<>).MakeGenericType(t))!;
    }

    private Type? GetCollectionType(Type t)
    {
        return IsReadOnlySetInterface(t) ? t.GetGenericArguments()[0] : null;

        static bool IsReadOnlySetInterface(Type t) => t.IsConstructedGenericType && t.GetGenericTypeDefinition() == typeof(IReadOnlySet<>);
    }
}
