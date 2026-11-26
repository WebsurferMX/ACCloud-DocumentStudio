namespace ACCloud.DocumentStudio.Rendering.Abstractions.Models;

/// <summary>
/// Línea horizontal si Width &gt;= Height; vertical en caso contrario.
/// </summary>
public sealed class RenderLineElement
    : RenderElement
{
    public string Color { get; set; } = "#000000";

    /// <summary>Grosor en puntos.</summary>
    public float Thickness { get; set; } = 1;
}
