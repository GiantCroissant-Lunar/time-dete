using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TimeDete.Traceability.HashChain;

public static class CanonicalJsonSerializer
{
    private static readonly JsonSerializerOptions BaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string Serialize<T>(T value)
    {
        if (value is null)
        {
            return "null";
        }

        var json = JsonSerializer.Serialize(value, BaseOptions);
        using var document = JsonDocument.Parse(json);
        return SerializeSorted(document.RootElement);
    }

    public static string Serialize(JsonDocument document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        return SerializeSorted(document.RootElement);
    }

    private static string SerializeSorted(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => SerializeObject(element),
        JsonValueKind.Array => SerializeArray(element),
        _ => element.GetRawText()
    };

    private static string SerializeObject(JsonElement obj)
    {
        var properties = obj
            .EnumerateObject()
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .Select(p =>
            {
                var encodedName = JsonEncodedText.Encode(p.Name, BaseOptions.Encoder).ToString();
                var value = SerializeSorted(p.Value);
                return $"\"{encodedName}\":{value}";
            });

        return "{" + string.Join(",", properties) + "}";
    }

    private static string SerializeArray(JsonElement array)
    {
        var items = array.EnumerateArray().Select(SerializeSorted);
        return "[" + string.Join(",", items) + "]";
    }
}
