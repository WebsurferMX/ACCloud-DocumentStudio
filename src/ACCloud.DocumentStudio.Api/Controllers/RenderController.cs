using ACCloud.DocumentStudio.Contracts.Render;
using ACCloud.DocumentStudio.Renderer.Interfaces;
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

    [HttpPost]
    public IActionResult Render(
        [FromBody] RenderRequest request)
    {
        var elements =
            _compiler.Compile(
                request.Template,
                request.Data);

        var pdf =
            _renderer.Render(elements);

        return File(
            pdf,
            "application/pdf",
            "document.pdf");
    }
}