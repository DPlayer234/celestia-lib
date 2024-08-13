using System;
using System.Diagnostics;
using System.Text.Json;
using CelestiaCS.Lib.Memory;

namespace CelestiaCS.Lib.Json;

internal static class JsonMerge
{
    // Based on: https://github.com/dotnet/runtime/issues/31433#issuecomment-570475853

    public static JsonDocument Merge(JsonDocument jDoc1, JsonDocument jDoc2)
    {
        using var stream = new ChunkedMemoryStream(128);
        using (var jsonWriter = new Utf8JsonWriter(stream))
        {
            JsonElement root1 = jDoc1.RootElement;
            JsonElement root2 = jDoc2.RootElement;

            if (root1.ValueKind != JsonValueKind.Object || root2.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException($"The original JSON document to merge new content into must be an object. Instead it is {root1.ValueKind}.");
            }

            MergeObjects(jsonWriter, root1, root2);
        }

        stream.Position = 0;
        return JsonDocument.Parse(stream);
    }

    private static void MergeObjects(Utf8JsonWriter jsonWriter, JsonElement root1, JsonElement root2)
    {
        Debug.Assert(root1.ValueKind == JsonValueKind.Object);
        Debug.Assert(root2.ValueKind == JsonValueKind.Object);

        jsonWriter.WriteStartObject();

        // Write all the properties of the first document.
        // If a property exists in both documents, either:
        // * Merge them, if the value kinds match (e.g. both are objects or arrays),
        // * Completely override the value of the first with the one from the second, if the value kind mismatches (e.g. one is object, while the other is an array or string)
        foreach (JsonProperty property in root1.EnumerateObject())
        {
            string propertyName = property.Name;

            if (root2.TryGetProperty(propertyName, out JsonElement newValue))
            {
                jsonWriter.WritePropertyName(propertyName);

                JsonElement originalValue = property.Value;
                JsonValueKind originalValueKind = originalValue.ValueKind;

                if (newValue.ValueKind == JsonValueKind.Object && originalValueKind == JsonValueKind.Object)
                {
                    MergeObjects(jsonWriter, originalValue, newValue);
                }
                else
                {
                    newValue.WriteTo(jsonWriter);
                }
            }
            else
            {
                property.WriteTo(jsonWriter);
            }
        }

        // Write all the properties of the second document that are unique to it.
        foreach (JsonProperty property in root2.EnumerateObject())
        {
            if (!root1.TryGetProperty(property.Name, out _))
            {
                property.WriteTo(jsonWriter);
            }
        }

        jsonWriter.WriteEndObject();
    }
}
