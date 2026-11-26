namespace ACCloud.DocumentStudio.Renderer.Models;

public sealed class RenderRectElement
    : RenderElement
{
    public string? BorderColor { get; set; }

    /// <summary>Grosor del borde en puntos; 0 = sin borde.</summary>
    public float BorderWidth { get; set; }

    public string? FillColor { get; set; }

    /// <summary>Radio de esquinas en mm.</summary>
    public float CornerRadius { get; set; }
}
