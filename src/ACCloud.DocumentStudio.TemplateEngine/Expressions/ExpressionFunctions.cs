using System.Globalization;
using System.Text.Json;
using ACCloud.DocumentStudio.Renderer;

namespace ACCloud.DocumentStudio.TemplateEngine.Expressions;

/// <summary>
/// Funciones disponibles en expresiones (los nombres no distinguen mayúsculas).
/// </summary>
internal static class ExpressionFunctions
{
    public static readonly IReadOnlyList<string> Names =
    [
        "ifNull", "if", "upper", "lower", "trim", "concat", "left", "right",
        "count", "sum", "formatNumber", "formatCurrency", "formatDate",
        "eq", "ne", "gt", "gte", "lt", "lte", "and", "or", "not",
        "importeConLetra"
    ];

    public static object? Invoke(string name, object?[] args)
    {
        switch (name.ToLowerInvariant())
        {
            case "ifnull":
                Arity(name, args, 2, 2);
                return ExpressionValues.IsEmpty(args[0]) ? args[1] : args[0];

            case "if":
                Arity(name, args, 2, 3);
                return ExpressionValues.IsTruthy(args[0]) ? args[1] : args.ElementAtOrDefault(2);

            case "upper":
                Arity(name, args, 1, 1);
                return ExpressionValues.ToText(args[0]).ToUpper(ExpressionValues.Mexico);

            case "lower":
                Arity(name, args, 1, 1);
                return ExpressionValues.ToText(args[0]).ToLower(ExpressionValues.Mexico);

            case "trim":
                Arity(name, args, 1, 1);
                return ExpressionValues.ToText(args[0]).Trim();

            case "concat":
                return string.Concat(args.Select(ExpressionValues.ToText));

            case "left":
            {
                Arity(name, args, 2, 2);
                var text = ExpressionValues.ToText(args[0]);
                var length = (int)Math.Clamp(ExpressionValues.ToDecimal(args[1]), 0, text.Length);
                return text[..length];
            }

            case "right":
            {
                Arity(name, args, 2, 2);
                var text = ExpressionValues.ToText(args[0]);
                var length = (int)Math.Clamp(ExpressionValues.ToDecimal(args[1]), 0, text.Length);
                return text[^length..];
            }

            case "count":
                Arity(name, args, 1, 1);
                return args[0] is JsonElement { ValueKind: JsonValueKind.Array } array
                    ? array.GetArrayLength()
                    : 0m;

            case "sum":
                Arity(name, args, 1, 2);
                return Sum(args[0], args.Length > 1 ? ExpressionValues.ToText(args[1]) : null);

            case "formatnumber":
                Arity(name, args, 1, 2);
                return ExpressionValues.TryToDecimal(args[0], out var number)
                    ? number.ToString(Format(args, 1, "N2"), ExpressionValues.Mexico)
                    : ExpressionValues.ToText(args[0]);

            case "formatcurrency":
                Arity(name, args, 1, 2);
                return ExpressionValues.TryToDecimal(args[0], out var amount)
                    ? amount.ToString("C2", ExpressionValues.Mexico)
                    : ExpressionValues.ToText(args[0]);

            case "formatdate":
                Arity(name, args, 1, 2);
                return FormatDate(args[0], Format(args, 1, "dd/MM/yyyy"));

            case "eq":
                Arity(name, args, 2, 2);
                return AreEqual(args[0], args[1]);

            case "ne":
                Arity(name, args, 2, 2);
                return !AreEqual(args[0], args[1]);

            case "gt":
                Arity(name, args, 2, 2);
                return ExpressionValues.ToDecimal(args[0]) > ExpressionValues.ToDecimal(args[1]);

            case "gte":
                Arity(name, args, 2, 2);
                return ExpressionValues.ToDecimal(args[0]) >= ExpressionValues.ToDecimal(args[1]);

            case "lt":
                Arity(name, args, 2, 2);
                return ExpressionValues.ToDecimal(args[0]) < ExpressionValues.ToDecimal(args[1]);

            case "lte":
                Arity(name, args, 2, 2);
                return ExpressionValues.ToDecimal(args[0]) <= ExpressionValues.ToDecimal(args[1]);

            case "and":
                return args.All(ExpressionValues.IsTruthy);

            case "or":
                return args.Any(ExpressionValues.IsTruthy);

            case "not":
                Arity(name, args, 1, 1);
                return !ExpressionValues.IsTruthy(args[0]);

            case "importeconletra":
                Arity(name, args, 1, 2);
                return NumberToWordsEs.Amount(
                    ExpressionValues.ToDecimal(args[0]),
                    args.Length > 1 ? ExpressionValues.ToText(args[1]) : "MXN");

            default:
                throw new DocumentValidationException(
                    $"Función desconocida '{name}'. Disponibles: {string.Join(", ", Names)}.");
        }
    }

    private static void Arity(string name, object?[] args, int min, int max)
    {
        if (args.Length < min || args.Length > max)
        {
            var expected = min == max ? $"{min}" : $"de {min} a {max}";

            throw new DocumentValidationException(
                $"La función '{name}' recibe {expected} argumento(s) y se le pasaron {args.Length}.");
        }
    }

    private static string Format(object?[] args, int index, string fallback)
    {
        return args.Length > index && args[index] is { } format
            ? ExpressionValues.ToText(format)
            : fallback;
    }

    private static decimal Sum(object? source, string? field)
    {
        if (source is not JsonElement { ValueKind: JsonValueKind.Array } array)
        {
            return 0;
        }

        decimal total = 0;

        foreach (var item in array.EnumerateArray())
        {
            var value = item;

            if (field is not null
                && (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty(field, out value)))
            {
                continue;
            }

            total += ExpressionValues.ToDecimal(ExpressionValues.FromJson(value));
        }

        return total;
    }

    private static string FormatDate(object? value, string format)
    {
        var text = ExpressionValues.ToText(value);

        return DateTime.TryParse(
            text,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out var date)
            ? date.ToString(format, ExpressionValues.Mexico)
            : text;
    }

    private static bool AreEqual(object? left, object? right)
    {
        if (left is decimal || right is decimal)
        {
            return ExpressionValues.TryToDecimal(left, out var a)
                   && ExpressionValues.TryToDecimal(right, out var b)
                   && a == b;
        }

        return string.Equals(
            ExpressionValues.ToText(left),
            ExpressionValues.ToText(right),
            StringComparison.Ordinal);
    }
}
