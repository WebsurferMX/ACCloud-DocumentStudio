namespace ACCloud.DocumentStudio.TemplateEngine.Models;

public class TemplateDefinition
{
    public PageDefinition Page { get; set; } = new();

    public IList<ElementDefinition> Elements { get; set; }
        = new List<ElementDefinition>();
}