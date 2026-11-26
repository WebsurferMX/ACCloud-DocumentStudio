using System.Text.Json;
using ACCloud.DocumentStudio.TemplateEngine.Models;

namespace ACCloud.DocumentStudio.UnitTests;

public class SectionJsonTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        AllowOutOfOrderMetadataProperties = true
    };

    [Fact]
    public void Deserializes_Sections_And_All_Element_Types()
    {
        const string json = """
        {
          "page": { "size": "letter" },
          "header": { "height": 20, "elements": [ { "type": "image", "source": "{{data.logo}}", "width": 30, "height": 15 } ] },
          "sections": [
            {
              "name": "items", "height": 8, "dataSource": "data.items",
              "elements": [
                { "x": 1, "type": "text", "value": "{{row.name}}" },
                { "type": "line", "width": 100, "thickness": 0.5 },
                { "type": "rect", "width": 10, "height": 5, "fillColor": "#EEEEEE" },
                { "type": "qr", "value": "{{row.url}}", "width": 20, "height": 20 },
                { "type": "barcode", "value": "{{row.sku}}", "format": "ean13", "width": 40, "height": 10 }
              ]
            }
          ],
          "footer": { "height": 6, "elements": [] }
        }
        """;

        var template = JsonSerializer.Deserialize<TemplateDefinition>(json, Options)!;

        Assert.IsType<ImageElementDefinition>(Assert.Single(template.Header!.Elements));
        var section = Assert.Single(template.Sections);
        Assert.Equal("data.items", section.DataSource);
        Assert.Collection(
            section.Elements,
            e => Assert.Equal(1f, Assert.IsType<TextElementDefinition>(e).X),
            e => Assert.IsType<LineElementDefinition>(e),
            e => Assert.IsType<RectElementDefinition>(e),
            e => Assert.IsType<QrElementDefinition>(e),
            e => Assert.Equal("ean13", Assert.IsType<BarcodeElementDefinition>(e).Format));
        Assert.NotNull(template.Footer);
    }
}
