namespace ACCloud.DocumentStudio.TemplateEngine.Models;

public sealed class QrElementDefinition
    : ElementDefinition
{
    /// <summary>Contenido del QR; admite expresiones.</summary>
    public string Value { get; set; } = string.Empty;
}
