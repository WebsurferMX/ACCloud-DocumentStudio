using System.Text.Json;
using ACCloud.DocumentStudio.Rendering.Abstractions;
using ACCloud.DocumentStudio.TemplateEngine.Models;

namespace ACCloud.DocumentStudio.TemplateEngine.Services;

/// <summary>Serialización estándar de plantillas (camelCase, "type" en cualquier posición).</summary>
public static class TemplateJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        AllowOutOfOrderMetadataProperties = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static string Serialize(TemplateDefinition template)
    {
        return JsonSerializer.Serialize(template, Options);
    }

    public static TemplateDefinition Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<TemplateDefinition>(json, Options)
                   ?? throw new DocumentValidationException("La plantilla está vacía.");
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            throw new DocumentValidationException($"La definición de la plantilla no es válida: {ex.Message}", ex);
        }
    }
}
