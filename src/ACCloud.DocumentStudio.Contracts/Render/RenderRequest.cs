using ACCloud.DocumentStudio.TemplateEngine.Models;

namespace ACCloud.DocumentStudio.Contracts.Render;

public sealed class RenderRequest
{
    public TemplateDefinition Template { get; set; }
        = new();

    public object Data { get; set; } = new();
}