using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using CelestiaCS.Lib.Collections;
using CelestiaCS.Lib.Localize;

namespace CelestiaCS.Lib.Json;

/// <summary>
/// A converter that can convert a <see cref="Localized{T}"/> to and from JSON.
/// </summary>
public class JsonLocalizedConverter<T> : JsonConverter<Localized<T>> where T : notnull
{
    /// <inheritdoc/>
    public override Localized<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            ThrowHelper.Json("Localized must be a map/object.");

        TinyDictionary<string, T> data = [];
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                ThrowHelper.Json("Unexpected token type. Expected property.");

            string culture = reader.GetString()!;
            if (!reader.Read())
                ThrowHelper.Json("Unexpected EOF.");

            T? value = JsonSerializer.Deserialize<T>(ref reader, options);
            if (value is null)
                ThrowHelper.Json("Localized value must not be null.");

            data.Add(culture, value);
        }

        return Localized<T>.CreateUnchecked(data);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Localized<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var entry in value.Enumerate())
        {
            writer.WritePropertyName(entry.Key);
            JsonSerializer.Serialize(writer, entry.Value, options);
        }

        writer.WriteEndObject();
    }
}

/// <summary>
/// A converter that can convert a <see cref="Localized{T}"/> of <see cref="string"/> to and from JSON.
/// </summary>
public sealed class JsonLocalizedStringConverter : JsonLocalizedConverter<string>
{
    /// <inheritdoc/>
    public override Localized<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            return base.Read(ref reader, typeToConvert, options);
        }

        return new Localized<string>(reader.GetString()!);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Localized<string> value, JsonSerializerOptions options)
    {
        if (value.LocalizedValues.Any())
        {
            base.Write(writer, value, options);
            return;
        }

        writer.WriteStringValue(value.InvariantValue);
    }
}

/// <summary>
/// Produces converters that can convert any <see cref="Localized{T}"/> to or from JSON.
/// </summary>
public sealed class JsonLocalizedConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert)
    {
        return GetInnerType(typeToConvert) != null;
    }

    /// <inheritdoc/>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var t = GetInnerType(typeToConvert);
        if (t is null) ThrowHelper.Argument(nameof(typeToConvert), "Type cannot be converted.");
        if (t == typeof(string)) return new JsonLocalizedStringConverter();
        return (JsonConverter)Activator.CreateInstance(typeof(JsonLocalizedConverter<>).MakeGenericType(t))!;
    }

    private Type? GetInnerType(Type t)
    {
        return IsLocalizedOfT(t) ? t.GetGenericArguments()[0] : null;

        static bool IsLocalizedOfT(Type t) => t.IsConstructedGenericType && t.GetGenericTypeDefinition() == typeof(Localized<>);
    }
}
