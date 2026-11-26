using System.Text.Json;

namespace ACCloud.DocumentStudio.Contracts.Documents;

public sealed class CreateDocumentRequest
{
    public Guid TemplateId { get; set; }

    /// <summary>Versión a usar; por defecto la publicada o, si no hay, la más reciente.</summary>
    public int? Version { get; set; }

    /// <summary>Datos del documento; si se omite se usan los datos de prueba de la versión.</summary>
    public JsonElement? Data { get; set; }

    /// <summary>pdf (por defecto) | png</summary>
    public string Format { get; set; } = "pdf";

    /// <summary>Solo png: página a devolver, desde 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Solo png: resolución.</summary>
    public int Dpi { get; set; } = 96;

    /// <summary>Nombre del archivo sin extensión.</summary>
    public string? FileName { get; set; }
}
