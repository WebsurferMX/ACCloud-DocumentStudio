using System.Text.Json;
using ACCloud.DocumentStudio.TemplateEngine.Models;

namespace ACCloud.DocumentStudio.Contracts.Render;

public sealed class RenderRequest
{
    public TemplateDefinition Template { get; set; }
        = new();

    /// <summary>JSON disponible en las expresiones como <c>data</c>.</summary>
    public JsonElement Data { get; set; }
}
