using ACCloud.DocumentStudio.Api.Services;
using ACCloud.DocumentStudio.Application.Templates;
using ACCloud.DocumentStudio.Contracts.Templates;
using Microsoft.AspNetCore.Mvc;

namespace ACCloud.DocumentStudio.Api.Controllers.V1;

/// <summary>Plantillas guardadas en la base, con historial de versiones.</summary>
[ApiController]
[Route("api/v1/templates")]
public sealed class TemplatesController : ControllerBase
{
    private readonly ITemplateService _templates;

    public TemplatesController(ITemplateService templates)
    {
        _templates = templates;
    }

    /// <summary>Crea una plantilla (versión 1).</summary>
    [HttpPost]
    public async Task<ActionResult<TemplateDetailResponse>> Create(
        [FromBody] CreateTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _templates.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Crea una plantilla copiando una de las incluidas (p. ej. cfdi-factura).</summary>
    [HttpPost("from-builtin")]
    public async Task<ActionResult<TemplateDetailResponse>> CreateFromBuiltIn(
        [FromBody] CreateFromBuiltInRequest request,
        [FromServices] BuiltInTemplateProvider builtIn,
        CancellationToken cancellationToken)
    {
        var created = await _templates.CreateAsync(
            new CreateTemplateRequest
            {
                Name = string.IsNullOrWhiteSpace(request.Name) ? request.BuiltIn : request.Name,
                Description = request.Description ?? $"Creada a partir de la plantilla incluida '{request.BuiltIn}'.",
                Definition = builtIn.Get(request.BuiltIn),
                SampleData = request.SampleData
            },
            cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Lista plantillas (más recientes primero).</summary>
    [HttpGet]
    public Task<PagedResponse<TemplateSummaryResponse>> List(
        [FromQuery] string? search,
        [FromQuery] bool includeArchived = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return _templates.ListAsync(search, includeArchived, page, pageSize, cancellationToken);
    }

    /// <summary>Plantilla con la definición de su versión más reciente.</summary>
    [HttpGet("{id:guid}")]
    public Task<TemplateDetailResponse> Get(Guid id, CancellationToken cancellationToken)
    {
        return _templates.GetAsync(id, cancellationToken);
    }

    /// <summary>Renombra y/o guarda una versión nueva (si se envía definition o sampleData).</summary>
    [HttpPut("{id:guid}")]
    public Task<TemplateDetailResponse> Update(
        Guid id,
        [FromBody] UpdateTemplateRequest request,
        CancellationToken cancellationToken)
    {
        return _templates.UpdateAsync(id, request, cancellationToken);
    }

    /// <summary>Archiva la plantilla (eliminación lógica).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        await _templates.ArchiveAsync(id, cancellationToken);

        return NoContent();
    }

    /// <summary>Publica una versión: será la que se use al generar si no se indica otra.</summary>
    [HttpPost("{id:guid}/publish")]
    public Task<TemplateDetailResponse> Publish(
        Guid id,
        [FromBody] PublishTemplateRequest? request,
        CancellationToken cancellationToken)
    {
        return _templates.PublishAsync(id, request?.Version, cancellationToken);
    }

    /// <summary>Quita la versión publicada: se generará con la más reciente.</summary>
    [HttpPost("{id:guid}/unpublish")]
    public Task<TemplateDetailResponse> Unpublish(Guid id, CancellationToken cancellationToken)
    {
        return _templates.UnpublishAsync(id, cancellationToken);
    }

    /// <summary>Historial de versiones.</summary>
    [HttpGet("{id:guid}/versions")]
    public Task<IReadOnlyList<TemplateVersionSummaryResponse>> Versions(Guid id, CancellationToken cancellationToken)
    {
        return _templates.ListVersionsAsync(id, cancellationToken);
    }

    /// <summary>Definición y datos de prueba de una versión.</summary>
    [HttpGet("{id:guid}/versions/{version:int}")]
    public Task<TemplateVersionResponse> Version(Guid id, int version, CancellationToken cancellationToken)
    {
        return _templates.GetVersionAsync(id, version, cancellationToken);
    }

    /// <summary>Crea una versión nueva igual a una anterior.</summary>
    [HttpPost("{id:guid}/versions/{version:int}/restore")]
    public Task<TemplateDetailResponse> Restore(Guid id, int version, CancellationToken cancellationToken)
    {
        return _templates.RestoreVersionAsync(id, version, cancellationToken);
    }
}
