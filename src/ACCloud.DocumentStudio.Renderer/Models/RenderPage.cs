namespace ACCloud.DocumentStudio.Renderer.Models;

public sealed class RenderPage
{
    public int Width { get; set; }

    public int Height { get; set; }

    public List<RenderElement> Elements { get; set; }
        = new();
}