using System.Text.Json.Serialization;

namespace ACCloud.DocumentStudio.TemplateEngine.Models;

[JsonPolymorphic(
    TypeDiscriminatorPropertyName = "type",
    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(TextElementDefinition), "text")]
public abstract class ElementDefinition
{
    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }
}
