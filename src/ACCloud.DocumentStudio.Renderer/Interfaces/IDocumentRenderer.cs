using ACCloud.DocumentStudio.Renderer.Models;

namespace ACCloud.DocumentStudio.Renderer.Interfaces;

public interface IDocumentRenderer
{
    /// <summary>Genera el PDF.</summary>
    Task<byte[]> RenderAsync(
        RenderDocument document,
        CancellationToken cancellationToken = default);

    /// <summary>Genera una imagen PNG por página (vista previa).</summary>
    Task<IReadOnlyList<byte[]>> RenderPreviewAsync(
        RenderDocument document,
        int dpi = 96,
        CancellationToken cancellationToken = default);
}
