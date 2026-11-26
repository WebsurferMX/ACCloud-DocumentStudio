namespace ACCloud.DocumentStudio.Renderer.Interfaces;

public interface IImageLoader
{
    /// <summary>
    /// Obtiene los bytes de una imagen a partir de una URL http(s),
    /// un data URI o base64. Regresa null si no se pudo obtener.
    /// </summary>
    Task<byte[]?> LoadAsync(
        string source,
        CancellationToken cancellationToken = default);
}
