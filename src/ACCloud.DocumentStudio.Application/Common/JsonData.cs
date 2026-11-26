using System.Text.Json;

namespace ACCloud.DocumentStudio.Application.Common;

/// <summary>Conversión entre JsonElement (API) y texto JSON (base de datos).</summary>
internal static class JsonData
{
    private static readonly JsonElement EmptyObject = JsonDocument.Parse("{}").RootElement.Clone();

    public static string? ToText(JsonElement? value)
    {
        return value is { ValueKind: not (JsonValueKind.Undefined or JsonValueKind.Null) } element
            ? element.GetRawText()
            : null;
    }

    public static JsonElement? FromText(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);

        return document.RootElement.Clone();
    }

    public static JsonElement OrEmpty(JsonElement? value)
    {
        return value is { ValueKind: not (JsonValueKind.Undefined or JsonValueKind.Null) } element
            ? element
            : EmptyObject;
    }
}
