namespace ACCloud.DocumentStudio.TemplateEngine.Models;

public sealed class RectElementDefinition
    : ElementDefinition
{
    public string? BorderColor { get; set; } = "#000000";

    /// <summary>Grosor del borde en puntos; 0 = sin borde.</summary>
    public float BorderWidth { get; set; }

    public string? FillColor { get; set; }

    /// <summary>Radio de esquinas en mm.</summary>
    public float CornerRadius { get; set; }
}
