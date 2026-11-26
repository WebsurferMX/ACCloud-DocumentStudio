using System.Text.Json;
using ACCloud.DocumentStudio.Rendering.Abstractions;
using ACCloud.DocumentStudio.Rendering.Abstractions.Models;
using ACCloud.DocumentStudio.TemplateEngine.Models;
using ACCloud.DocumentStudio.TemplateEngine.Services;

namespace ACCloud.DocumentStudio.UnitTests;

public class TemplateCompilerTests
{
    private static readonly TemplateCompiler Compiler = new(new TemplateResolver());

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void Data_Bound_Section_Repeats_Per_Row()
    {
        var template = new TemplateDefinition
        {
            Sections =
            {
                new SectionDefinition
                {
                    Height = 8,
                    DataSource = "data.items",
                    Elements = { new TextElementDefinition { Value = "{{rowNumber}}. {{row.name}}", Width = 50, Height = 5 } }
                }
            }
        };

        var document = Compiler.Compile(template, Json("""{ "items": [ { "name": "A" }, { "name": "B" }, { "name": "C" } ] }"""));

        Assert.Equal(3, document.Body.Count);
        Assert.Equal(
            ["1. A", "2. B", "3. C"],
            document.Body.Select(b => ((RenderTextElement)b.Elements[0]).Text));
    }

    [Fact]
    public void VisibleIf_Hides_Elements_And_Rows()
    {
        var template = new TemplateDefinition
        {
            Sections =
            {
                new SectionDefinition
                {
                    Height = 8,
                    DataSource = "{{data.items}}",
                    VisibleIf = "gt(row.qty, 0)",
                    Elements =
                    {
                        new TextElementDefinition { Value = "{{row.name}}", Width = 50, Height = 5 },
                        new TextElementDefinition { Value = "oferta", Width = 50, Height = 5, VisibleIf = "row.promo" }
                    }
                }
            }
        };

        var document = Compiler.Compile(template, Json("""
            { "items": [ { "name": "A", "qty": 1, "promo": true }, { "name": "B", "qty": 0 }, { "name": "C", "qty": 2 } ] }
            """));

        Assert.Equal(2, document.Body.Count);
        Assert.Equal(2, document.Body[0].Elements.Count);
        Assert.Single(document.Body[1].Elements);
    }

    [Fact]
    public void Section_Height_Defaults_To_Lowest_Element()
    {
        var template = new TemplateDefinition
        {
            Sections =
            {
                new SectionDefinition
                {
                    Elements =
                    {
                        new TextElementDefinition { Y = 2, Height = 5 },
                        new RectElementDefinition { Y = 10, Height = 4 }
                    }
                }
            }
        };

        Assert.Equal(14, Compiler.Compile(template, Json("{}")).Body[0].Height);
    }

    [Fact]
    public void Legacy_Elements_Become_First_Body_Band()
    {
        var template = new TemplateDefinition
        {
            Elements = { new TextElementDefinition { Value = "Hola {{nombre}}", Width = 50, Height = 6 } }
        };

        var document = Compiler.Compile(template, Json("""{ "nombre": "Mundo" }"""));

        Assert.Equal("Hola Mundo", ((RenderTextElement)Assert.Single(document.Body).Elements[0]).Text);
    }

    [Fact]
    public void Page_Size_Preset_And_Orientation()
    {
        var template = new TemplateDefinition { Page = new PageDefinition { Size = "a4", Orientation = "landscape" } };

        var document = Compiler.Compile(template, Json("{}"));

        Assert.Equal(297f, document.PageWidth);
        Assert.Equal(210f, document.PageHeight);
    }

    [Fact]
    public void DataSource_That_Is_Not_An_Array_Is_Rejected()
    {
        var template = new TemplateDefinition
        {
            Sections = { new SectionDefinition { Name = "x", Height = 5, DataSource = "data.total" } }
        };

        Assert.Throws<DocumentValidationException>(() => Compiler.Compile(template, Json("""{ "total": 5 }""")));
    }
}
