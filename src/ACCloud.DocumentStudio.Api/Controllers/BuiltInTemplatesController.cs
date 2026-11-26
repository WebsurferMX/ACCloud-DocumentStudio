using ACCloud.DocumentStudio.Api.Services;
using ACCloud.DocumentStudio.TemplateEngine.Models;
using Microsoft.AspNetCore.Mvc;

namespace ACCloud.DocumentStudio.Api.Controllers;

/// <summary>Plantillas incluidas con la API (galería).</summary>
[ApiController]
[Route("api/templates/builtin")]
public sealed class BuiltInTemplatesController : ControllerBase
{
    private readonly BuiltInTemplateProvider _templates;

    public BuiltInTemplatesController(BuiltInTemplateProvider templates)
    {
        _templates = templates;
    }

    [HttpGet]
    public IReadOnlyCollection<string> List() => _templates.Names;

    [HttpGet("{name}")]
    public TemplateDefinition Get(string name) => _templates.Get(name);
}
