namespace ACCloud.DocumentStudio.TemplateEngine.Models;

/// <summary>
/// Plantilla de documento organizada en secciones (bandas).
/// Todas las medidas están en milímetros; los tamaños de fuente en puntos.
/// </summary>
public class TemplateDefinition
{
    public PageDefinition Page { get; set; } = new();

    /// <summary>Se repite en cada página.</summary>
    public SectionDefinition? Header { get; set; }

    /// <summary>Secciones del cuerpo, en orden.</summary>
    public IList<SectionDefinition> Sections { get; set; }
        = new List<SectionDefinition>();

    /// <summary>Se repite en cada página.</summary>
    public SectionDefinition? Footer { get; set; }

    /// <summary>
    /// Compatibilidad con el contrato 0.2: elementos sin sección.
    /// Se tratan como una sección de cuerpo previa a <see cref="Sections"/>.
    /// </summary>
    public IList<ElementDefinition> Elements { get; set; }
        = new List<ElementDefinition>();
}
