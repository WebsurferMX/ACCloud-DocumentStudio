namespace ACCloud.DocumentStudio.Rendering.Abstractions.Models;

public sealed class RenderImageElement
    : RenderElement
{
    /// <summary>URL http(s), data URI o base64.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>contain | fill | width | height</summary>
    public string Fit { get; set; } = "contain";
}
