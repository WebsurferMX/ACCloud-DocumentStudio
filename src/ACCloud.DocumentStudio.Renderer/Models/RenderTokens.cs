namespace ACCloud.DocumentStudio.Renderer.Models;

/// <summary>
/// Marcadores que el compilador deja en el texto para valores que solo se
/// conocen al paginar (<c>{{sys.pageNumber}}</c>, <c>{{sys.totalPages}}</c>).
/// Usan caracteres de uso privado de Unicode para no chocar con texto real.
/// </summary>
public static class RenderTokens
{
    public const char PageNumber = '';

    public const char TotalPages = '';
}
