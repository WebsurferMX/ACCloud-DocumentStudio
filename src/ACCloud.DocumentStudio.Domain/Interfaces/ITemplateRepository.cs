using ACCloud.DocumentStudio.Domain.Entities;

namespace ACCloud.DocumentStudio.Domain.Interfaces;

public interface ITemplateRepository
{
    /// <summary>Plantilla sin sus versiones (incluye archivadas).</summary>
    Task<Template?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<TemplateVersion?> GetVersionAsync(
        Guid templateId,
        int versionNumber,
        CancellationToken cancellationToken = default);

    /// <summary>Historial completo, de la más reciente a la más antigua.</summary>
    Task<IReadOnlyList<TemplateVersion>> ListVersionsAsync(
        Guid templateId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Template> Items, int Total)> ListAsync(
        string? search,
        bool includeArchived,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    void Add(Template template);

    /// <summary>
    /// Guarda la unidad de trabajo. Lanza DomainConflictException si otra
    /// operación concurrente creó la misma versión.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
