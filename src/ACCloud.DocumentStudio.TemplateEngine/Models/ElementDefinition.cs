using System.Text.Json.Serialization;

namespace ACCloud.DocumentStudio.TemplateEngine.Models;

[JsonPolymorphic(
    TypeDiscriminatorPropertyName = "type",
    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(TextElementDefinition), "text")]
[JsonDerivedType(typeof(ImageElementDefinition), "image")]
[JsonDerivedType(typeof(LineElementDefinition), "line")]
[JsonDerivedType(typeof(RectElementDefinition), "rect")]
[JsonDerivedType(typeof(QrElementDefinition), "qr")]
[JsonDerivedType(typeof(BarcodeElementDefinition), "barcode")]
public abstract class ElementDefinition
{
    /// <summary>Nombre del elemento (lo usa el editor).</summary>
    public string? Id { get; set; }

    /// <summary>Posición en mm, relativa a la sección.</summary>
    public float X { get; set; }

    public float Y { get; set; }

    /// <summary>Tamaño en mm.</summary>
    public float Width { get; set; }

    public float Height { get; set; }

    /// <summary>Orden de dibujo; mayor = encima.</summary>
    public int ZIndex { get; set; }

    public bool Visible { get; set; } = true;

    /// <summary>Expresión; si es falsa el elemento no se dibuja.</summary>
    public string? VisibleIf { get; set; }
}
