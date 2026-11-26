namespace ACCloud.DocumentStudio.Renderer.Models;

public sealed class RenderTextElement
    : RenderElement
{
    public string Text { get; set; }
        = string.Empty;

    public int FontSize { get; set; }
        = 12;
}