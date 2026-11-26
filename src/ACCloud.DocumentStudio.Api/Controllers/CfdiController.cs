using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ACCloud.DocumentStudio.Api.Services;
using ACCloud.DocumentStudio.Contracts.Cfdi;
using ACCloud.DocumentStudio.Renderer.Interfaces;
using ACCloud.DocumentStudio.TemplateEngine.Cfdi;
using ACCloud.DocumentStudio.TemplateEngine.Interfaces;
using ACCloud.DocumentStudio.TemplateEngine.Models;
using Microsoft.AspNetCore.Mvc;

namespace ACCloud.DocumentStudio.Api.Controllers;

/// <summary>
/// Representación impresa de CFDI. Acepta el XML dentro de un JSON
/// (application/json) o directo en el body (application/xml).
/// </summary>
[ApiController]
[Route("api/cfdi")]
public sealed class CfdiController : ControllerBase
{
    public const string DefaultTemplate = "cfdi-factura";

    private readonly ITemplateCompiler _compiler;
    private readonly IDocumentRenderer _renderer;
    private readonly BuiltInTemplateProvider _templates;

    public CfdiController(
        ITemplateCompiler compiler,
        IDocumentRenderer renderer,
        BuiltInTemplateProvider templates)
    {
        _compiler = compiler;
        _renderer = renderer;
        _templates = templates;
    }

    /// <summary>Convierte el XML al JSON que reciben las plantillas (útil para diseñar).</summary>
    [HttpPost("data")]
    [Consumes("application/json")]
    public ActionResult<JsonObject> Data([FromBody] CfdiDataRequest request)
    {
        return CfdiXmlConverter.Convert(request.Xml);
    }

    /// <summary>Igual que el anterior, con el XML directo en el body.</summary>
    [HttpPost("data")]
    [Consumes("application/xml", "text/xml")]
    public async Task<ActionResult<JsonObject>> DataFromXml(CancellationToken cancellationToken)
    {
        return CfdiXmlConverter.Convert(await ReadBodyAsync(cancellationToken));
    }

    /// <summary>Genera el PDF del CFDI con una plantilla propia o incluida.</summary>
    [HttpPost("render")]
    [Consumes("application/json")]
    [Produces("application/pdf")]
    public Task<IActionResult> Render(
        [FromBody] CfdiRenderRequest request,
        CancellationToken cancellationToken)
    {
        var template = request.Template ?? _templates.Get(request.TemplateName ?? DefaultTemplate);

        return RenderAsync(request.Xml, template, cancellationToken);
    }

    /// <summary>Genera el PDF con el XML directo en el body.</summary>
    [HttpPost("render")]
    [Consumes("application/xml", "text/xml")]
    [Produces("application/pdf")]
    public async Task<IActionResult> RenderFromXml(
        [FromQuery] string? template,
        CancellationToken cancellationToken)
    {
        var xml = await ReadBodyAsync(cancellationToken);

        return await RenderAsync(xml, _templates.Get(template ?? DefaultTemplate), cancellationToken);
    }

    private async Task<IActionResult> RenderAsync(
        string xml,
        TemplateDefinition template,
        CancellationToken cancellationToken)
    {
        var data = CfdiXmlConverter.Convert(xml);
        var document = _compiler.Compile(template, JsonSerializer.SerializeToElement(data));
        var pdf = await _renderer.RenderAsync(document, cancellationToken);

        var uuid = data["timbre"]?["uuid"]?.GetValue<string>();
        var fileName = string.IsNullOrEmpty(uuid) ? "cfdi.pdf" : $"{uuid}.pdf";

        return File(pdf, "application/pdf", fileName);
    }

    private async Task<string> ReadBodyAsync(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);

        return await reader.ReadToEndAsync(cancellationToken);
    }
}
