namespace ACCloud.DocumentStudio.Renderer.Models;

/// <summary>
/// Franja de altura fija; sus elementos se posicionan de forma absoluta
/// respecto a la esquina superior izquierda de la banda.
/// </summary>
public sealed class RenderBand
{
    public float Height { get; set; }

    public List<RenderElement> Elements { get; set; } = new();
}
