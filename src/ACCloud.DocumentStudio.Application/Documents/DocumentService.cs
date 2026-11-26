using System.Text.Json;
using ACCloud.DocumentStudio.Application.Common;
using ACCloud.DocumentStudio.Application.Exceptions;
using ACCloud.DocumentStudio.Contracts.Documents;
using ACCloud.DocumentStudio.Domain.Interfaces;
using ACCloud.DocumentStudio.TemplateEngine.Services;

namespace ACCloud.DocumentStudio.Application.Documents;

public sealed class DocumentService : IDocumentService
{
    private readonly ITemplateRepository _repository;
    private readonly IDocumentGenerator _generator;

    public DocumentService(
        ITemplateRepository repository,
        IDocumentGenerator generator)
    {
        _repository = repository;
        _generator = generator;
    }

    public async Task<RenderedDocument> RenderAsync(
        CreateDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var stored = await GetStoredTemplateAsync(request.TemplateId, request.Version, cancellationToken);

        var data = request.Data is { ValueKind: not (JsonValueKind.Undefined or JsonValueKind.Null) }
            ? request.Data.Value
            : JsonData.OrEmpty(stored.SampleData);

        return await _generator.GenerateAsync(
            stored.Definition,
            data,
            request.Format,
            request.FileName ?? stored.Name,
            request.Page,
            request.Dpi,
            cancellationToken);
    }

    public async Task<StoredTemplate> GetStoredTemplateAsync(
        Guid templateId,
        int? version,
        CancellationToken cancellationToken = default)
    {
        var template = await _repository.GetAsync(templateId, cancellationToken);

        if (template is null || template.IsArchived)
        {
            throw NotFoundException.Template(templateId);
        }

        var number = template.ResolveVersion(version);

        var entity = await _repository.GetVersionAsync(templateId, number, cancellationToken)
                     ?? throw NotFoundException.Version(templateId, number);

        return new StoredTemplate(
            template.Id,
            template.Name,
            number,
            TemplateJson.Deserialize(entity.DefinitionJson),
            JsonData.FromText(entity.SampleDataJson));
    }
}
