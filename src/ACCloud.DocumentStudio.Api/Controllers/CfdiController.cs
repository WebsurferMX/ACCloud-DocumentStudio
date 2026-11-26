using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ACCloud.DocumentStudio.Api.Services;
using ACCloud.DocumentStudio.Application.Documents;
using ACCloud.DocumentStudio.Contracts.Cfdi;
using ACCloud.DocumentStudio.TemplateEngine.Cfdi;
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

    private readonly IDocumentGenerator _generator;
    private readonly BuiltInTemplateProvider _builtIn;

    public CfdiController(
        IDocumentGenerator generator,
        BuiltInTemplateProvider builtIn)
    {
        _generator = generator;
        _builtIn = builtIn;
    }

    /// <summary>
    /// Convierte el XML al JSON que reciben las plantillas (útil para diseñar).
    /// También acepta el XML directo con Content-Type application/xml.
    /// </summary>
    [HttpPost("data")]
    [Consumes("application/json")]
    public ActionResult<JsonObject> Data([FromBody] CfdiDataRequest request)
    {
        return CfdiXmlConverter.Convert(request.Xml);
    }

    /// <summary>Igual que el anterior, con el XML directo en el body.</summary>
    /// <remarks>Oculto en Swagger: OpenAPI no admite dos acciones con el mismo método y ruta.</remarks>
    [HttpPost("data")]
    [Consumes("application/xml", "text/xml")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<JsonObject>> DataFromXml(CancellationToken cancellationToken)
    {
        return CfdiXmlConverter.Convert(await ReadBodyAsync(cancellationToken));
    }

    /// <summary>
    /// Genera el PDF del CFDI. Plantilla: templateId (guardada), template (en la
    /// petición) o templateName (incluida; por defecto cfdi-factura).
    /// También acepta el XML directo con Content-Type application/xml y la
    /// plantilla por query: ?templateId=&amp;version= o ?template=.
    /// </summary>
    [HttpPost("render")]
    [Consumes("application/json")]
    [Produces("application/pdf")]
    public async Task<IActionResult> Render(
        [FromBody] CfdiRenderRequest request,
        CancellationToken cancellationToken)
    {
        TemplateDefinition template;

        if (request.TemplateId is { } templateId)
        {
            template = await StoredTemplateAsync(templateId, request.Version, cancellationToken);
        }
        else
        {
            template = request.Template ?? _builtIn.Get(request.TemplateName ?? DefaultTemplate);
        }

        return await RenderAsync(request.Xml, template, cancellationToken);
    }

    /// <summary>
    /// Genera el PDF con el XML directo en el body. Plantilla por query:
    /// templateId (+ version) o template (incluida).
    /// </summary>
    [HttpPost("render")]
    [Consumes("application/xml", "text/xml")]
    [Produces("application/pdf")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> RenderFromXml(
        [FromQuery] Guid? templateId,
        [FromQuery] int? version,
        [FromQuery] string? template,
        CancellationToken cancellationToken)
    {
        var xml = await ReadBodyAsync(cancellationToken);

        var definition = templateId is { } id
            ? await StoredTemplateAsync(id, version, cancellationToken)
            : _builtIn.Get(template ?? DefaultTemplate);

        return await RenderAsync(xml, definition, cancellationToken);
    }

    private async Task<IActionResult> RenderAsync(
        string xml,
        TemplateDefinition template,
        CancellationToken cancellationToken)
    {
        var data = CfdiXmlConverter.Convert(xml);
        var uuid = data["timbre"]?["uuid"]?.GetValue<string>();

        var document = await _generator.GenerateAsync(
            template,
            JsonSerializer.SerializeToElement(data),
            "pdf",
            string.IsNullOrEmpty(uuid) ? "cfdi" : uuid,
            cancellationToken: cancellationToken);

        return File(document.Content, document.ContentType, document.FileName);
    }

    /// <summary>
    /// Se resuelve aquí (no en el constructor) para que las plantillas incluidas
    /// o enviadas en la petición funcionen aunque no haya base de datos.
    /// </summary>
    private async Task<TemplateDefinition> StoredTemplateAsync(
        Guid templateId,
        int? version,
        CancellationToken cancellationToken)
    {
        var documents = HttpContext.RequestServices.GetRequiredService<IDocumentService>();

        return (await documents.GetStoredTemplateAsync(templateId, version, cancellationToken)).Definition;
    }

    private async Task<string> ReadBodyAsync(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);

        return await reader.ReadToEndAsync(cancellationToken);
    }
}
