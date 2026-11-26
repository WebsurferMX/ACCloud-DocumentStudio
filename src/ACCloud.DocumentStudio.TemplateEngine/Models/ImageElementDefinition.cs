namespace ACCloud.DocumentStudio.TemplateEngine.Models;

public sealed class ImageElementDefinition
    : ElementDefinition
{
    /// <summary>URL http(s), data URI o base64; admite expresiones.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>contain | fill | width | height</summary>
    public string Fit { get; set; } = "contain";
}
