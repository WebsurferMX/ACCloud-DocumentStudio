using ACCloud.DocumentStudio.TemplateEngine.Expressions;

namespace ACCloud.DocumentStudio.TemplateEngine.Interfaces;

public interface ITemplateResolver
{
    /// <summary>Reemplaza cada <c>{{ expresión }}</c> del texto por su valor.</summary>
    string Resolve(
        string template,
        ExpressionContext context);

    /// <summary>Atajo para resolver contra un objeto cualquiera como <c>data</c>.</summary>
    string Resolve(
        string template,
        object data);

    /// <summary>
    /// Evalúa una expresión (con o sin <c>{{ }}</c>) y regresa su valor:
    /// null, string, decimal, bool o JsonElement.
    /// </summary>
    object? Evaluate(
        string expression,
        ExpressionContext context);
}
