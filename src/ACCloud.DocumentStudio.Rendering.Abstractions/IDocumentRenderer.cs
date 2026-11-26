using ACCloud.DocumentStudio.Rendering.Abstractions.Models;

namespace ACCloud.DocumentStudio.Rendering.Abstractions;

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
