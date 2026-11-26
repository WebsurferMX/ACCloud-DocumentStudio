using ACCloud.DocumentStudio.Rendering.Abstractions;
using ACCloud.DocumentStudio.TemplateEngine.Models;
using ACCloud.DocumentStudio.TemplateEngine.Services;

namespace ACCloud.DocumentStudio.Api.Services;

/// <summary>
/// Plantillas incluidas con la API (carpeta Templates/*.json), cargadas al iniciar.
/// </summary>
public sealed class BuiltInTemplateProvider
{
    private readonly Dictionary<string, TemplateDefinition> _templates;

    public BuiltInTemplateProvider()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "Templates");

        _templates = Directory.Exists(folder)
            ? Directory.EnumerateFiles(folder, "*.json").ToDictionary(
                file => Path.GetFileNameWithoutExtension(file),
                file => TemplateJson.Deserialize(File.ReadAllText(file)),
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
