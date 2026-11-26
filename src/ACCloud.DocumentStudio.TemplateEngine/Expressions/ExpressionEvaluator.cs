using System.Globalization;
using System.Text.Json;
using ACCloud.DocumentStudio.Renderer.Models;

namespace ACCloud.DocumentStudio.TemplateEngine.Expressions;

internal static class ExpressionEvaluator
{
    public static object? Evaluate(Expr expression, ExpressionContext context)
    {
        return expression switch
        {
            LiteralExpr literal => literal.Value,
            PathExpr path => ResolvePath(path.Segments, context),
            CallExpr call => ExpressionFunctions.Invoke(
                call.Name,
                call.Arguments.Select(argument => Evaluate(argument, context)).ToArray()),
            _ => null
        };
    }

    private static object? ResolvePath(
        IReadOnlyList<PathSegment> segments,
        ExpressionContext context)
    {
        var root = segments[0].Name;

        switch (root)
        {
            case "data":
                return Walk(context.Data, segments, 1);

            case "row":
                return context.Row is { } row ? Walk(row, segments, 1) : null;

            case "rowIndex" when segments.Count == 1:
                return context.RowIndex is { } index ? (decimal)index : null;

            case "rowNumber" when segments.Count == 1:
                return context.RowIndex is { } number ? (decimal)(number + 1) : null;

            case "sys":
                return ResolveSystem(segments);

            default:
                // Compatibilidad: {{cliente.nombre}} equivale a {{data.cliente.nombre}}.
                return Walk(context.Data, segments, 0);
        }
    }

    private static object? Walk(
        JsonElement current,
        IReadOnlyList<PathSegment> segments,
        int start)
    {
        for (var i = start; i < segments.Count; i++)
        {
            var segment = segments[i];

            if (segment.Name is { } name)
            {
                if (current.ValueKind != JsonValueKind.Object
                    || !current.TryGetProperty(name, out current))
                {
                    return null;
                }
            }
            else if (segment.Index is { } index)
            {
                if (current.ValueKind != JsonValueKind.Array
                    || index >= current.GetArrayLength())
                {
                    return null;
                }

                current = current[index];
            }
        }

        return ExpressionValues.FromJson(current);
    }

    private static object? ResolveSystem(IReadOnlyList<PathSegment> segments)
    {
        if (segments.Count != 2)
        {
            return null;
        }

        return segments[1].Name switch
        {
            "pageNumber" => RenderTokens.PageNumber.ToString(),
            "totalPages" => RenderTokens.TotalPages.ToString(),
            "date" => DateTime.Now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            "now" => DateTime.Now.ToString("s", CultureInfo.InvariantCulture),
            _ => null
        };
    }
}
