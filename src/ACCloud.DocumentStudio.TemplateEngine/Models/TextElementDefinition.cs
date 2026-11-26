namespace ACCloud.DocumentStudio.TemplateEngine.Models;

public sealed class TextElementDefinition
    : ElementDefinition
{
    public string Value { get; set; } = string.Empty;

    public int FontSize { get; set; } = 12;
}