namespace ACCloud.DocumentStudio.Renderer.Models;

/// <summary>
/// Documento ya resuelto (sin expresiones), listo para dibujar.
/// Todas las medidas están en milímetros.
/// </summary>
public sealed class RenderDocument
{
    public float PageWidth { get; set; } = 215.9f;

    public float PageHeight { get; set; } = 279.4f;

    public RenderMargin Margin { get; set; } = new(10, 10, 10, 10);

    /// <summary>Se repite en cada página.</summary>
    public RenderBand? Header { get; set; }

    /// <summary>Bandas del cuerpo en orden; QuestPDF las pagina.</summary>
    public List<RenderBand> Body { get; set; } = new();

    /// <summary>Se repite en cada página.</summary>
    public RenderBand? Footer { get; set; }
}
