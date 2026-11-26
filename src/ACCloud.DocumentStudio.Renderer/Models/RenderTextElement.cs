namespace ACCloud.DocumentStudio.Renderer.Models;

public sealed class RenderTextElement
    : RenderElement
{
    /// <summary>Texto resuelto; puede contener <see cref="RenderTokens"/>.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Tamaño en puntos.</summary>
    public float FontSize { get; set; } = 10;

    public bool Bold { get; set; }

    public bool Italic { get; set; }

    public string Color { get; set; } = "#000000";

    public string? BackgroundColor { get; set; }

    /// <summary>left | center | right</summary>
    public string Align { get; set; } = "left";

    /// <summary>top | middle | bottom</summary>
    public string VerticalAlign { get; set; } = "top";

    public string? FontFamily { get; set; }

    /// <summary>Permite cortar palabras largas (sellos digitales, cadenas).</summary>
    public bool WrapAnywhere { get; set; }
}
