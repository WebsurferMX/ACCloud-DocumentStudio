using ACCloud.DocumentStudio.Application.Common;
using ACCloud.DocumentStudio.Application.Exceptions;
using ACCloud.DocumentStudio.Contracts.Templates;
using ACCloud.DocumentStudio.Domain.Entities;
using ACCloud.DocumentStudio.Domain.Interfaces;
using ACCloud.DocumentStudio.TemplateEngine.Interfaces;
using ACCloud.DocumentStudio.TemplateEngine.Models;
using ACCloud.DocumentStudio.TemplateEngine.Services;

namespace ACCloud.DocumentStudio.Application.Templates;

public sealed class TemplateService : ITemplateService
{
    public const int MaxPageSize = 100;

    private readonly ITemplateRepository _repository;
    private readonly ITemplateCompiler _compiler;
    private readonly TimeProvider _clock;

    public TemplateService(
        ITemplateRepository repository,
        ITemplateCompiler compiler,
        TimeProvider clock)
    {
        _repository = repository;
        _compiler = compiler;
        _clock = clock;
    }

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    public async Task<TemplateDetailResponse> CreateAsync(
        CreateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var sampleData = JsonData.ToText(request.SampleData);
        var definitionJson = Validate(request.Definition, sampleData);

        var template = Template.Create(request.Name, request.Description, definitionJson, sampleData, Now);

        _repository.Add(template);
        await _repository.SaveChangesAsync(cancellationToken);

        return ToDetail(template, template.Versions.Last());
    }

    public async Task<PagedResponse<TemplateSummaryResponse>> ListAsync(
        string? search,
        bool includeArchived,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var (items, total) = await _repository.ListAsync(
            search,
            includeArchived,
            (page - 1) * pageSize,
            pageSize,
            cancellationToken);

        return new PagedResponse<TemplateSummaryResponse>
        {
            Items = items.Select(ToSummary).ToList(),
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<TemplateDetailResponse> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var template = await LoadAsync(id, cancellationToken);
        var version = await LoadVersionAsync(id, template.CurrentVersion, cancellationToken);

        return ToDetail(template, version);
    }

    public async Task<TemplateDetailResponse> UpdateAsync(
        Guid id,
        UpdateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var template = await LoadAsync(id, cancellationToken);

        if (request.Name is not null || request.Description is not null)
        {
            template.Rename(request.Name ?? template.Name, request.Description ?? template.Description, Now);
        }

        var current = await LoadVersionAsync(id, template.CurrentVersion, cancellationToken);
        var latest = current;

        if (request.Definition is not null || request.SampleData.HasValue)
        {
            var sampleData = request.SampleData.HasValue
                ? JsonData.ToText(request.SampleData)
                : current.SampleDataJson;

            var definitionJson = request.Definition is not null
                ? Validate(request.Definition, sampleData)
                : current.DefinitionJson;

            latest = template.AddVersion(definitionJson, sampleData, request.Comment, Now);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return ToDetail(template, latest);
    }

    public async Task ArchiveAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var template = await LoadAsync(id, cancellationToken);

        template.Archive(Now);

        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<TemplateDetailResponse> PublishAsync(
        Guid id,
        int? version,
        CancellationToken cancellationToken = default)
    {
        var template = await LoadAsync(id, cancellationToken);

        template.Publish(version ?? template.CurrentVersion, Now);

        await _repository.SaveChangesAsync(cancellationToken);

        return ToDetail(template, await LoadVersionAsync(id, template.CurrentVersion, cancellationToken));
    }

    public async Task<TemplateDetailResponse> UnpublishAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var template = await LoadAsync(id, cancellationToken);

        template.Unpublish(Now);

        await _repository.SaveChangesAsync(cancellationToken);

        return ToDetail(template, await LoadVersionAsync(id, template.CurrentVersion, cancellationToken));
    }

    public async Task<IReadOnlyList<TemplateVersionSummaryResponse>> ListVersionsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var template = await LoadAsync(id, cancellationToken);
        var versions = await _repository.ListVersionsAsync(id, cancellationToken);

        return versions
            .Select(v => new TemplateVersionSummaryResponse
            {
                Version = v.VersionNumber,
                Comment = v.Comment,
                CreatedAt = v.CreatedAt,
                IsCurrent = v.VersionNumber == template.CurrentVersion,
                IsPublished = v.VersionNumber == template.PublishedVersion
            })
            .ToList();
    }

    public async Task<TemplateVersionResponse> GetVersionAsync(
        Guid id,
        int version,
        CancellationToken cancellationToken = default)
    {
        var template = await LoadAsync(id, cancellationToken);
        var entity = await LoadVersionAsync(id, version, cancellationToken);

        return new TemplateVersionResponse
        {
            TemplateId = id,
            Version = entity.VersionNumber,
            Comment = entity.Comment,
            CreatedAt = entity.CreatedAt,
            IsCurrent = entity.VersionNumber == template.CurrentVersion,
            IsPublished = entity.VersionNumber == template.PublishedVersion,
            Definition = TemplateJson.Deserialize(entity.DefinitionJson),
            SampleData = JsonData.FromText(entity.SampleDataJson)
        };
    }

    public async Task<TemplateDetailResponse> RestoreVersionAsync(
        Guid id,
        int version,
        CancellationToken cancellationToken = default)
    {
        var template = await LoadAsync(id, cancellationToken);
        var source = await LoadVersionAsync(id, version, cancellationToken);

        var restored = template.AddVersion(
            source.DefinitionJson,
            source.SampleDataJson,
            $"Restaurada desde la versión {version}",
            Now);

        await _repository.SaveChangesAsync(cancellationToken);

        return ToDetail(template, restored);
    }

    /// <summary>
    /// Compila la plantilla con sus datos de prueba (o un objeto vacío) para detectar
    /// expresiones inválidas antes de guardar. Devuelve el JSON normalizado.
    /// </summary>
    private string Validate(TemplateDefinition? definition, string? sampleDataJson)
    {
        if (definition is null)
        {
            throw new Domain.Exceptions.DomainValidationException("La definición de la plantilla es obligatoria.");
        }

        _compiler.Compile(definition, JsonData.OrEmpty(JsonData.FromText(sampleDataJson)));

        return TemplateJson.Serialize(definition);
    }

    private async Task<Template> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _repository.GetAsync(id, cancellationToken)
               ?? throw NotFoundException.Template(id);
    }

    private async Task<TemplateVersion> LoadVersionAsync(Guid id, int version, CancellationToken cancellationToken)
    {
        return await _repository.GetVersionAsync(id, version, cancellationToken)
               ?? throw NotFoundException.Version(id, version);
    }

    internal static TemplateSummaryResponse ToSummary(Template template)
    {
        return new TemplateSummaryResponse
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Status = template.Status.ToString(),
            CurrentVersion = template.CurrentVersion,
            PublishedVersion = template.PublishedVersion,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }

    private static TemplateDetailResponse ToDetail(Template template, TemplateVersion version)
    {
        return new TemplateDetailResponse
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Status = template.Status.ToString(),
            CurrentVersion = template.CurrentVersion,
            PublishedVersion = template.PublishedVersion,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
            Definition = TemplateJson.Deserialize(version.DefinitionJson),
            SampleData = JsonData.FromText(version.SampleDataJson)
        };
    }
}
