using System.Text.Json;
using ACCloud.DocumentStudio.Contracts.Documents;
using ACCloud.DocumentStudio.TemplateEngine.Models;

namespace ACCloud.DocumentStudio.Application.Documents;

/// <summary>Archivo generado.</summary>
public sealed record RenderedDocument(
    byte[] Content,
    string ContentType,
    string FileName,
    int? TotalPages);

/// <summary>Plantilla guardada, ya resuelta a una versión concreta.</summary>
public sealed record StoredTemplate(
    Guid TemplateId,
    string Name,
    int Version,
    TemplateDefinition Definition,
    JsonElement? SampleData);

/// <summary>Genera documentos a partir de una plantilla en memoria. No usa base de datos.</summary>
public interface IDocumentGenerator
{
    Task<RenderedDocument> GenerateAsync(
        TemplateDefinition template,
        JsonElement data,
        string format,
        string fileName,
        int page = 1,
        int dpi = 96,
        CancellationToken cancellationToken = default);
}

/// <summary>Genera documentos con plantillas guardadas en la base.</summary>
public interface IDocumentService
{
    Task<RenderedDocument> RenderAsync(
        CreateDocumentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Carga una plantilla guardada (no archivada) en la versión a usar.</summary>
    Task<StoredTemplate> GetStoredTemplateAsync(
        Guid templateId,
        int? version,
        CancellationToken cancellationToken = default);
}
