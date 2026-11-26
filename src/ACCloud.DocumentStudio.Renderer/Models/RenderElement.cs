namespace ACCloud.DocumentStudio.Renderer.Models;

/// <summary>Elemento posicionado dentro de una banda (medidas en mm).</summary>
public abstract class RenderElement
{
    public float X { get; set; }

    public float Y { get; set; }

    public float Width { get; set; }

    public float Height { get; set; }

    public int ZIndex { get; set; }
}
