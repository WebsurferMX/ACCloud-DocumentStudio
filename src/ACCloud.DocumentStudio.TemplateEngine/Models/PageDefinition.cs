namespace ACCloud.DocumentStudio.TemplateEngine.Models;

public class PageDefinition
{
    /// <summary>
    /// Tamaño predefinido: letter, legal, a4, a5. Si se indica,
    /// reemplaza a <see cref="Width"/> y <see cref="Height"/>.
    /// </summary>
    public string? Size { get; set; }

    /// <summary>portrait | landscape (solo aplica con <see cref="Size"/>).</summary>
    public string? Orientation { get; set; }

    /// <summary>Ancho en mm (por defecto carta).</summary>
    public float Width { get; set; } = 215.9f;

    /// <summary>Alto en mm (por defecto carta).</summary>
    public float Height { get; set; } = 279.4f;

    public MarginDefinition Margin { get; set; } = new();
}
