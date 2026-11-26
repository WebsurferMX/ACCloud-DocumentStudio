using System.Text.Json;
using ACCloud.DocumentStudio.TemplateEngine.Models;

namespace ACCloud.DocumentStudio.Contracts.Templates;

public sealed class CreateTemplateRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TemplateDefinition Definition { get; set; } = new();

    /// <summary>JSON de prueba (pestaña "Data" del editor y vista previa).</summary>
    public JsonElement? SampleData { get; set; }
}

public sealed class UpdateTemplateRequest
{
    /// <summary>Si se omite se conserva el actual.</summary>
    public string? Name { get; set; }

    /// <summary>Si se omite se conserva la actual.</summary>
    public string? Description { get; set; }

    /// <summary>Si se envía se crea una versión nueva.</summary>
    public TemplateDefinition? Definition { get; set; }

    /// <summary>Datos de prueba de la nueva versión; si se omite se copian los de la versión actual.</summary>
    public JsonElement? SampleData { get; set; }

    /// <summary>Comentario de la versión ("Ajuste de márgenes").</summary>
    public string? Comment { get; set; }
}

public sealed class PublishTemplateRequest
{
    /// <summary>Versión a publicar; si se omite, la más reciente.</summary>
    public int? Version { get; set; }
}

public sealed class CreateFromBuiltInRequest
{
    /// <summary>Nombre de la plantilla incluida (p. ej. cfdi-factura).</summary>
    public string BuiltIn { get; set; } = string.Empty;

    /// <summary>Nombre de la nueva plantilla; por defecto el de la incluida.</summary>
    public string? Name { get; set; }

    public string? Description { get; set; }

    public JsonElement? SampleData { get; set; }
}
