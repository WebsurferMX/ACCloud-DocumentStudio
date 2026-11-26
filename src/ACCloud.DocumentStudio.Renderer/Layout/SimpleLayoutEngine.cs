using ACCloud.DocumentStudio.Renderer.Models;

namespace ACCloud.DocumentStudio.Renderer.Layout;

public sealed class SimpleLayoutEngine
    : ILayoutEngine
{
    public RenderPage Build(
        RenderPage page)
    {
        return page;
    }
}