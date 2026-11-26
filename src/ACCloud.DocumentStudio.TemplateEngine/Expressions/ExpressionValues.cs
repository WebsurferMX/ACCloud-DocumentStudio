using System.Globalization;
using System.Text.Json;

namespace ACCloud.DocumentStudio.TemplateEngine.Expressions;

/// <summary>
/// Conversión de valores de expresiones. Los valores posibles son:
/// null, string, decimal, bool y JsonElement (objetos y arreglos).
/// </summary>
public static class ExpressionValues
{
    public static readonly CultureInfo Mexico = CultureInfo.GetCultureInfo("es-MX");

    public static string ToText(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string s => s,
            decimal d => d.ToString(CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            JsonElement e => e.GetRawText(),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    public static bool TryToDecimal(object? value, out decimal result)
    {
        switch (value)
        {
            case decimal d:
                result = d;
                return true;
            case bool b:
                result = b ? 1 : 0;
                return true;
            case string s when decimal.TryParse(
                s.Trim(),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var parsed):
                result = parsed;
                return true;
            default:
                result = 0;
                return false;
        }
    }

    public static decimal ToDecimal(object? value)
    {
        return TryToDecimal(value, out var result) ? result : 0;
    }

    public static bool IsEmpty(object? value)
    {
        return value is null
            || (value is string s && string.IsNullOrWhiteSpace(s))
            || (value is JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined });
    }

    public static bool IsTruthy(object? value)
    {
        return value switch
        {
            null => false,
            bool b => b,
            decimal d => d != 0,
            string s => !string.IsNullOrWhiteSpace(s)
                        && !s.Equals("false", StringComparison.OrdinalIgnoreCase)
                        && s != "0",
            JsonElement { ValueKind: JsonValueKind.Array } e => e.GetArrayLength() > 0,
            JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } => false,
            _ => true
        };
    }

    /// <summary>Convierte un JsonElement escalar a string/decimal/bool/null.</summary>
    internal static object? FromJson(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetDecimal(out var d) ? d : (decimal)element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => element
        };
    }
}
