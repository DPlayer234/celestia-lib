using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using CelestiaCS.Lib.Format;

namespace CelestiaCS.Lib.Json;

public sealed class JsonFormatStringConverter : JsonConverter<FormatString>
{
    public override FormatString Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => new FormatString(reader.GetString()),
            JsonTokenType.StartArray => new FormatString(string.Join('\n', JsonSerializer.Deserialize<string[]>(ref reader, options)!)),
            _ => ThrowInvalidTokenType()
        };
    }

    public override void Write(Utf8JsonWriter writer, FormatString value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }

    private static FormatString ThrowInvalidTokenType()
    {
        throw new JsonException("FormatString must be a string in JSON.");
    }
}
