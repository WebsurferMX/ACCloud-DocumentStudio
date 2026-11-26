using ACCloud.DocumentStudio.TemplateEngine.Models;

namespace ACCloud.DocumentStudio.Contracts.Cfdi;

public sealed class CfdiRenderRequest
{
    /// <summary>XML completo del CFDI (3.3 o 4.0), timbrado.</summary>
    public string Xml { get; set; } = string.Empty;

    /// <summary>
    /// Plantilla guardada en la base. Prioridad: <see cref="TemplateId"/>,
    /// luego <see cref="Template"/>, luego <see cref="TemplateName"/>.
    /// </summary>
    public Guid? TemplateId { get; set; }

    /// <summary>Versión de la plantilla guardada; por defecto la publicada o la más reciente.</summary>
    public int? Version { get; set; }

    /// <summary>Plantilla enviada en la petición.</summary>
    public TemplateDefinition? Template { get; set; }

    /// <summary>Nombre de una plantilla incluida (por defecto <c>cfdi-factura</c>).</summary>
    public string? TemplateName { get; set; }
}
