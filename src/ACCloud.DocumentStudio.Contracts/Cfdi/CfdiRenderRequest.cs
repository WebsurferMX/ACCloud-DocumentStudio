using ACCloud.DocumentStudio.TemplateEngine.Models;

namespace ACCloud.DocumentStudio.Contracts.Cfdi;

public sealed class CfdiRenderRequest
{
    /// <summary>XML completo del CFDI (3.3 o 4.0), timbrado.</summary>
    public string Xml { get; set; } = string.Empty;

    /// <summary>Plantilla propia. Si se omite se usa <see cref="TemplateName"/>.</summary>
    public TemplateDefinition? Template { get; set; }

    /// <summary>Nombre de una plantilla incluida (por defecto <c>cfdi-factura</c>).</summary>
    public string? TemplateName { get; set; }
}
