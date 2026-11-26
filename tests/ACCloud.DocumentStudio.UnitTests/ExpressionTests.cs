using System.Text.Json;
using ACCloud.DocumentStudio.Rendering.Abstractions;
using ACCloud.DocumentStudio.Rendering.Abstractions.Models;
using ACCloud.DocumentStudio.TemplateEngine.Expressions;
using ACCloud.DocumentStudio.TemplateEngine.Services;

namespace ACCloud.DocumentStudio.UnitTests;

public class ExpressionTests
{
    private static readonly TemplateResolver Resolver = new();

    private static readonly JsonElement Data = JsonDocument.Parse("""
        {
          "cliente": { "nombre": "ACME", "rfc": null },
          "total": 1234.5,
          "fecha": "2026-11-25T10:30:00",
          "items": [
            { "descripcion": "Uno", "importe": 10.25 },
            { "descripcion": "Dos", "importe": 4.75 }
          ]
        }
        """).RootElement;

    private static string Resolve(string template, ExpressionContext? context = null)
        => Resolver.Resolve(template, context ?? new ExpressionContext(Data));

    [Theory]
    [InlineData("{{data.cliente.nombre}}", "ACME")]
    [InlineData("{{cliente.nombre}}", "ACME")]
    [InlineData("{{ data.items[1].descripcion }}", "Dos")]
    [InlineData("{{data.noExiste.campo}}", "")]
    [InlineData("Total: {{data.total}}", "Total: 1234.5")]
    public void Resolves_Paths(string template, string expected)
    {
        Assert.Equal(expected, Resolve(template));
    }

    [Fact]
    public void Resolves_Row_Context()
    {
        var items = Data.GetProperty("items");
        var context = new ExpressionContext(Data).ForRow(items[1], 1);

        Assert.Equal("Dos #2 (1)", Resolve("{{row.descripcion}} #{{rowNumber}} ({{rowIndex}})", context));
    }

    [Theory]
    [InlineData("{{ ifNull(data.cliente.rfc, \"XAXX010101000\") }}", "XAXX010101000")]
    [InlineData("{{ ifNull(data.cliente.nombre, \"-\") }}", "ACME")]
    [InlineData("{{ formatCurrency(data.total) }}", "$1,234.50")]
    [InlineData("{{ formatNumber(data.total, \"N3\") }}", "1,234.500")]
    [InlineData("{{ formatDate(data.fecha, \"dd/MM/yyyy\") }}", "25/11/2026")]
    [InlineData("{{ sum(data.items, \"importe\") }}", "15.00")]
    [InlineData("{{ count(data.items) }}", "2")]
    [InlineData("{{ upper(data.cliente.nombre) }}", "ACME")]
    [InlineData("{{ concat(\"A\", \"-\", 1) }}", "A-1")]
    [InlineData("{{ right(\"ABCDEFGHIJ\", 3) }}", "HIJ")]
    [InlineData("{{ if(gt(data.total, 1000), \"alto\", \"bajo\") }}", "alto")]
    [InlineData("{{ if(eq(data.cliente.nombre, 'ACME'), 'sí', 'no') }}", "sí")]
    public void Evaluates_Functions(string template, string expected)
    {
        Assert.Equal(expected, Resolve(template));
    }

    [Fact]
    public void System_Values_Become_Render_Tokens()
    {
        var text = Resolve("Página {{sys.pageNumber}} de {{sys.totalPages}}");

        Assert.Equal($"Página {RenderTokens.PageNumber} de {RenderTokens.TotalPages}", text);
    }

    [Theory]
    [InlineData("{{ formatCurrency(data.total }}")]
    [InlineData("{{ noExiste(1) }}")]
    [InlineData("{{ ifNull(data.total) }}")]
    [InlineData("{{ 'sin cerrar }}")]
    public void Invalid_Expressions_Throw_Validation_Error(string template)
    {
        Assert.Throws<DocumentValidationException>(() => Resolve(template));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("0", false)]
    [InlineData("false", false)]
    [InlineData("texto", true)]
    public void Truthiness(string? value, bool expected)
    {
        Assert.Equal(expected, ExpressionValues.IsTruthy(value));
    }
}
