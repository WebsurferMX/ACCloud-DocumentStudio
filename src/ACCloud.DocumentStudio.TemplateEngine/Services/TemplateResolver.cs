using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using ACCloud.DocumentStudio.TemplateEngine.Expressions;
using ACCloud.DocumentStudio.TemplateEngine.Interfaces;

namespace ACCloud.DocumentStudio.TemplateEngine.Services;

public sealed partial class TemplateResolver
    : ITemplateResolver
{
    private const int MaxCachedExpressions = 10_000;

    private static readonly ConcurrentDictionary<string, Expr> Cache = new(StringComparer.Ordinal);

    public string Resolve(
        string template,
        ExpressionContext context)
    {
        if (string.IsNullOrEmpty(template)
            || !template.Contains("{{", StringComparison.Ordinal))
        {
            return template ?? string.Empty;
        }

        return Placeholder().Replace(
            template,
            match => ExpressionValues.ToText(Evaluate(match.Groups[1].Value, context)));
    }

    public string Resolve(
        string template,
        object data)
    {
        return Resolve(template, ExpressionContext.From(data));
    }

    public object? Evaluate(
        string expression,
        ExpressionContext context)
    {
        var text = Unwrap(expression);

        if (Cache.Count > MaxCachedExpressions)
        {
            Cache.Clear();
        }

        var parsed = Cache.GetOrAdd(text, ExpressionParser.Parse);

        return ExpressionEvaluator.Evaluate(parsed, context);
    }

    private static string Unwrap(string expression)
    {
        var text = expression.Trim();

        if (text.StartsWith("{{", StringComparison.Ordinal)
            && text.EndsWith("}}", StringComparison.Ordinal))
        {
            text = text[2..^2].Trim();
        }

        return text;
    }

    [GeneratedRegex(@"\{\{(.+?)\}\}", RegexOptions.Singleline)]
    private static partial Regex Placeholder();
}
