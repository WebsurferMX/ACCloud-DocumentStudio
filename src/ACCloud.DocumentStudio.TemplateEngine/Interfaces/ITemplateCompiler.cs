using ACCloud.DocumentStudio.Renderer.Models;
using ACCloud.DocumentStudio.TemplateEngine.Models;

namespace ACCloud.DocumentStudio.TemplateEngine.Interfaces;

public interface ITemplateCompiler
{
    List<RenderTextElement> Compile(
        TemplateDefinition template,
        object data);
}