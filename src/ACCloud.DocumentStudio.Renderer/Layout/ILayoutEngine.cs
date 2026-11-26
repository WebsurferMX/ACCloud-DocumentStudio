using ACCloud.DocumentStudio.Renderer.Models;

namespace ACCloud.DocumentStudio.Renderer.Layout;

public interface ILayoutEngine
{
    RenderPage Build(
        RenderPage page);
}