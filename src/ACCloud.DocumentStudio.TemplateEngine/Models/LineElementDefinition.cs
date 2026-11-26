namespace ACCloud.DocumentStudio.TemplateEngine.Models;

/// <summary>Horizontal si Width &gt;= Height; vertical en caso contrario.</summary>
public sealed class LineElementDefinition
    : ElementDefinition
{
    public string Color { get; set; } = "#000000";

    /// <summary>Grosor en puntos.</summary>
    public float Thickness { get; set; } = 1;
}
