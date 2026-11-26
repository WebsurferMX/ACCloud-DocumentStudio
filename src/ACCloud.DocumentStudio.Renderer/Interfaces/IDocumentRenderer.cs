using ACCloud.DocumentStudio.Renderer.Models;

namespace ACCloud.DocumentStudio.Renderer.Interfaces;

public interface IDocumentRenderer
{
    byte[] Render(
        List<RenderTextElement> elements);
}