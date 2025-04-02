using System;
using System.Collections.Generic;
using System.Text.Json;

namespace MyDMVpro.Common;

public static class JsonHelper
{
    public static Dictionary<string, object> DeserializeJsonToDictionary(string jsonString)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        using (JsonDocument doc = JsonDocument.Parse(jsonString, new JsonDocumentOptions { AllowTrailingCommas = true }))
        {
            return ParseElement(doc.RootElement);
        }
    }

    private static Dictionary<string, object> ParseElement(JsonElement element)
    {
        Dictionary<string, object> dictionary = new();

        foreach (JsonProperty property in element.EnumerateObject())
        {
            dictionary[property.Name] = ParseValue(property.Value);
        }

        return dictionary;
    }

    private static object ParseValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ParseElement(element),
            JsonValueKind.Array => ParseArray(element),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out long l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => throw new NotSupportedException($"Unsupported JSON value kind: {element.ValueKind}"),
        };
    }

    private static List<object> ParseArray(JsonElement element)
    {
        List<object> array = new();

        foreach (JsonElement item in element.EnumerateArray())
        {
            array.Add(ParseValue(item));
        }

        return array;
    }
    // Function to serialize Dictionary<string, object> to JSON string
    public static string SerializeDictionaryToJson(Dictionary<string, object> dictionary)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true // This formats the JSON string with indentation for readability
        };

        return JsonSerializer.Serialize(TransformDictionary(dictionary), options);
    }

    private static Dictionary<string, object> TransformDictionary(Dictionary<string, object> dictionary)
    {
        var transformedDict = new Dictionary<string, object>();

        foreach (var kvp in dictionary)
        {
            transformedDict[kvp.Key] = TransformValue(kvp.Value);
        }

        return transformedDict;
    }

    private static object TransformValue(object value)
    {
        return value switch
        {
            Dictionary<string, object> nestedDict => TransformDictionary(nestedDict),
            List<object> list => TransformList(list),
            _ => value
        };
    }

    private static List<object> TransformList(List<object> list)
    {
        List<object> transformedList = new();

        foreach (var item in list)
        {
            transformedList.Add(TransformValue(item));
        }

        return transformedList;
    }
}
