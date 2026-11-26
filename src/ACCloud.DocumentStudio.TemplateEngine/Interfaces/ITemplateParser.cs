using ACCloud.DocumentStudio.TemplateEngine.Models;

namespace ACCloud.DocumentStudio.TemplateEngine.Interfaces;

public interface ITemplateParser
{
    TemplateDefinition Parse(
        string json);
}