namespace ACCloud.DocumentStudio.TemplateEngine.Models;

/// <summary>
/// Banda de altura fija. Si tiene <see cref="DataSource"/>, se repite una vez
/// por cada elemento del arreglo y dentro de ella está disponible <c>row</c>.
/// </summary>
public class SectionDefinition
{
    public string? Name { get; set; }

    /// <summary>Alto en mm. Si es 0 se calcula con el elemento más bajo.</summary>
    public float Height { get; set; }

    /// <summary>Expresión que regresa un arreglo, p. ej. <c>data.conceptos</c>.</summary>
    public string? DataSource { get; set; }

    /// <summary>Expresión; si es falsa la sección (o el renglón) no se dibuja.</summary>
    public string? VisibleIf { get; set; }

    public IList<ElementDefinition> Elements { get; set; }
        = new List<ElementDefinition>();
}
