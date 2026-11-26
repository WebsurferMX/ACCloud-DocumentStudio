namespace ACCloud.DocumentStudio.TemplateEngine.Models;

public sealed class TextElementDefinition
    : ElementDefinition
{
    /// <summary>Texto con expresiones <c>{{ ... }}</c>.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Tamaño en puntos.</summary>
    public float FontSize { get; set; } = 10;

    public bool Bold { get; set; }

    public bool Italic { get; set; }

    public string Color { get; set; } = "#000000";

    public string? BackgroundColor { get; set; }

    /// <summary>left | center | right</summary>
    public string Align { get; set; } = "left";

    /// <summary>top | middle | bottom</summary>
    public string VerticalAlign { get; set; } = "top";

    public string? FontFamily { get; set; }

    /// <summary>Corta palabras largas en cualquier carácter (sellos digitales).</summary>
    public bool WrapAnywhere { get; set; }
}
