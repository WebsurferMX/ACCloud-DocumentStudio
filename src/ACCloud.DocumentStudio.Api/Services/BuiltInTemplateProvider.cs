using System.Text.Json;
using ACCloud.DocumentStudio.Renderer;
using ACCloud.DocumentStudio.TemplateEngine.Models;

namespace ACCloud.DocumentStudio.Api.Services;

/// <summary>
/// Plantillas incluidas con la API (carpeta Templates/*.json), cargadas al iniciar.
/// </summary>
public sealed class BuiltInTemplateProvider
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        AllowOutOfOrderMetadataProperties = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly Dictionary<string, TemplateDefinition> _templates;

    public BuiltInTemplateProvider()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "Templates");

        _templates = Directory.Exists(folder)
            ? Directory.EnumerateFiles(folder, "*.json").ToDictionary(
                file => Path.GetFileNameWithoutExtension(file),
                file => JsonSerializer.Deserialize<TemplateDefinition>(File.ReadAllText(file), JsonOptions)
                        ?? throw new InvalidOperationException($"Plantilla vacía: {file}"),
                StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, TemplateDefinition>(StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<string> Names => _templates.Keys;

    public TemplateDefinition Get(string name)
    {
        return _templates.TryGetValue(name, out var template)
            ? template
            : throw new DocumentValidationException(
                $"La plantilla '{name}' no existe. Disponibles: {string.Join(", ", _templates.Keys)}.");
    }
}
