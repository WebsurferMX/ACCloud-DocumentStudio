namespace ACCloud.DocumentStudio.Domain.Entities;

/// <summary>
/// Versión inmutable de una plantilla. Cada guardado crea una nueva.
/// </summary>
public sealed class TemplateVersion
{
    private TemplateVersion()
    {
    }

    internal TemplateVersion(
        Guid templateId,
        int versionNumber,
        string definitionJson,
        string? sampleDataJson,
        string? comment,
        DateTime createdAt)
    {
        Id = Guid.NewGuid();
        TemplateId = templateId;
        VersionNumber = versionNumber;
        DefinitionJson = definitionJson;
        SampleDataJson = sampleDataJson;
        Comment = comment;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid TemplateId { get; private set; }

    /// <summary>Consecutivo por plantilla, desde 1.</summary>
    public int VersionNumber { get; private set; }

    /// <summary>Definición de la plantilla (JSON de TemplateDefinition).</summary>
    public string DefinitionJson { get; private set; } = string.Empty;

    /// <summary>JSON de prueba para el editor y la vista previa.</summary>
    public string? SampleDataJson { get; private set; }

    public string? Comment { get; private set; }

    public DateTime CreatedAt { get; private set; }
}
