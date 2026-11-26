using ACCloud.DocumentStudio.Contracts.Templates;

namespace ACCloud.DocumentStudio.Application.Templates;

public interface ITemplateService
{
    Task<TemplateDetailResponse> CreateAsync(
        CreateTemplateRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<TemplateSummaryResponse>> ListAsync(
        string? search,
        bool includeArchived,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<TemplateDetailResponse> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>Renombra y, si viene definición o datos de prueba, crea una versión nueva.</summary>
    Task<TemplateDetailResponse> UpdateAsync(
        Guid id,
        UpdateTemplateRequest request,
        CancellationToken cancellationToken = default);

    Task ArchiveAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<TemplateDetailResponse> PublishAsync(
        Guid id,
        int? version,
        CancellationToken cancellationToken = default);

    Task<TemplateDetailResponse> UnpublishAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TemplateVersionSummaryResponse>> ListVersionsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<TemplateVersionResponse> GetVersionAsync(
        Guid id,
        int version,
        CancellationToken cancellationToken = default);

    /// <summary>Crea una versión nueva copiando una anterior (rollback sin perder historial).</summary>
    Task<TemplateDetailResponse> RestoreVersionAsync(
        Guid id,
        int version,
        CancellationToken cancellationToken = default);
}
