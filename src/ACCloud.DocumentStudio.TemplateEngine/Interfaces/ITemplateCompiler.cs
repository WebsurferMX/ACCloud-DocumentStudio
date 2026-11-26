using System.Text.Json;
using ACCloud.DocumentStudio.Rendering.Abstractions.Models;
using ACCloud.DocumentStudio.TemplateEngine.Models;

namespace ACCloud.DocumentStudio.TemplateEngine.Interfaces;

public interface ITemplateCompiler
{
    /// <summary>
    /// Resuelve expresiones, repite secciones con dataSource y aplica
    /// visibilidad, produciendo un documento listo para dibujar.
    /// </summary>
    RenderDocument Compile(
        TemplateDefinition template,
        JsonElement data);
}
