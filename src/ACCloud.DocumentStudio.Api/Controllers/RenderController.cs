using ACCloud.DocumentStudio.Contracts.Render;
using ACCloud.DocumentStudio.Rendering.Abstractions;
using ACCloud.DocumentStudio.TemplateEngine.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ACCloud.DocumentStudio.Api.Controllers;

[ApiController]
[Route("api/render")]
public sealed class RenderController : ControllerBase
{
    private readonly ITemplateCompiler _compiler;
    private readonly IDocumentRenderer _renderer;

    public RenderController(
        ITemplateCompiler compiler,
        IDocumentRenderer renderer)
    {
        _compiler = compiler;
        _renderer = renderer;
    }

    /// <summary>Genera un PDF a partir de una plantilla y datos JSON.</summary>
    [HttpPost]
    [Produces("application/pdf")]
    public async Task<IActionResult> Render(
        [FromBody] RenderRequest request,
        CancellationToken cancellationToken)
    {
        var document = _compiler.Compile(request.Template, request.Data);
        var pdf = await _renderer.RenderAsync(document, cancellationToken);

        return File(pdf, "application/pdf", "document.pdf");
    }

    /// <summary>Vista previa: PNG de la página indicada (desde 1).</summary>
    [HttpPost("preview")]
    [Produces("image/png")]
    public async Task<IActionResult> Preview(
        [FromBody] RenderRequest request,
        [FromQuery] int page = 1,
        [FromQuery] int dpi = 96,
        CancellationToken cancellationToken = default)
    {
        var document = _compiler.Compile(request.Template, request.Data);
        var pages = await _renderer.RenderPreviewAsync(document, dpi, cancellationToken);

        if (page < 1 || page > pages.Count)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Página fuera de rango",
                detail: $"El documento tiene {pages.Count} página(s).");
        }

        Response.Headers["X-Total-Pages"] = pages.Count.ToString();

        return File(pages[page - 1], "image/png");
    }
}
