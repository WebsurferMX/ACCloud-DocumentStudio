using ACCloud.DocumentStudio.Application.Documents;
using ACCloud.DocumentStudio.Contracts.Documents;
using Microsoft.AspNetCore.Mvc;

namespace ACCloud.DocumentStudio.Api.Controllers.V1;

/// <summary>Generación de documentos con plantillas guardadas.</summary>
[ApiController]
[Route("api/v1/documents")]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documents;

    public DocumentsController(IDocumentService documents)
    {
        _documents = documents;
    }

    /// <summary>
    /// Genera un PDF (o PNG de una página) con la plantilla indicada.
    /// Si no se envía data se usan los datos de prueba de la versión.
    /// </summary>
    [HttpPost]
    [Produces("application/pdf", "image/png")]
    public async Task<IActionResult> Create(
        [FromBody] CreateDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var document = await _documents.RenderAsync(request, cancellationToken);

        if (document.TotalPages is { } pages)
        {
            Response.Headers["X-Total-Pages"] = pages.ToString();
        }

        return File(document.Content, document.ContentType, document.FileName);
    }
}
