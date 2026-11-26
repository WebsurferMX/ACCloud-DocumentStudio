using System.Text.Json;
using ACCloud.DocumentStudio.TemplateEngine.Models;

namespace ACCloud.DocumentStudio.UnitTests;

public class TemplateDefinitionJsonTests
{
    private static readonly JsonSerializerOptions WebOptions =
        new(JsonSerializerDefaults.Web);

    [Fact]
    public void Should_Deserialize_Text_Element_By_Type_Discriminator()
    {
        const string json = """
        {
          "page": { "width": 600, "height": 800 },
          "elements": [
            { "type": "text", "value": "Hola {{nombre}}", "fontSize": 14, "x": 10 }
          ]
        }
        """;

        var template =
            JsonSerializer.Deserialize<TemplateDefinition>(json, WebOptions);

        Assert.NotNull(template);
        var element = Assert.Single(template!.Elements);
        var text = Assert.IsType<TextElementDefinition>(element);
        Assert.Equal("Hola {{nombre}}", text.Value);
        Assert.Equal(14, text.FontSize);
        Assert.Equal(10, text.X);
    }

    [Fact]
    public void Should_Reject_Element_Without_Type()
    {
        const string json = """
        { "elements": [ { "value": "sin tipo" } ] }
        """;

        Assert.ThrowsAny<NotSupportedException>(() =>
            JsonSerializer.Deserialize<TemplateDefinition>(json, WebOptions));
    }
}
