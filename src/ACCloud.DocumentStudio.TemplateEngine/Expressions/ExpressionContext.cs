using System.Text.Json;

namespace ACCloud.DocumentStudio.TemplateEngine.Expressions;

/// <summary>
/// Datos visibles para una expresión:
/// <list type="bullet">
/// <item><c>data</c>: el JSON completo del documento.</item>
/// <item><c>row</c>, <c>rowIndex</c> (desde 0), <c>rowNumber</c> (desde 1): solo dentro de una sección con dataSource.</item>
/// <item><c>sys</c>: <c>pageNumber</c>, <c>totalPages</c>, <c>date</c>, <c>now</c>.</item>
/// </list>
/// </summary>
public sealed class ExpressionContext
{
    private static readonly JsonElement EmptyObject =
        JsonDocument.Parse("{}").RootElement.Clone();

    public ExpressionContext(
        JsonElement data,
        JsonElement? row = null,
        int? rowIndex = null)
    {
        Data = data.ValueKind == JsonValueKind.Undefined ? EmptyObject : data;
        Row = row;
        RowIndex = rowIndex;
    }

    public JsonElement Data { get; }

    public JsonElement? Row { get; }

    public int? RowIndex { get; }

    public ExpressionContext ForRow(JsonElement row, int rowIndex)
    {
        return new ExpressionContext(Data, row, rowIndex);
    }

    public static ExpressionContext From(object? data)
    {
        return data switch
        {
            null => new ExpressionContext(EmptyObject),
            JsonElement element => new ExpressionContext(element),
            _ => new ExpressionContext(JsonSerializer.SerializeToElement(data))
        };
    }
}
