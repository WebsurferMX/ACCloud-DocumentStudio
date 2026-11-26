namespace ACCloud.DocumentStudio.TemplateEngine.Models;

public sealed class BarcodeElementDefinition
    : ElementDefinition
{
    /// <summary>Contenido; admite expresiones.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>code128 | code39 | ean13 | ean8 | upca | pdf417 | datamatrix</summary>
    public string Format { get; set; } = "code128";
}
