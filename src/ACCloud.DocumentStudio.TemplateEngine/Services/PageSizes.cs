using ACCloud.DocumentStudio.Renderer;
using ACCloud.DocumentStudio.TemplateEngine.Models;

namespace ACCloud.DocumentStudio.TemplateEngine.Services;

/// <summary>Tamaños de página predefinidos, en mm.</summary>
internal static class PageSizes
{
    public static (float Width, float Height) Resolve(PageDefinition page)
    {
        if (string.IsNullOrWhiteSpace(page.Size))
        {
            return (page.Width, page.Height);
        }

        (float width, float height) = page.Size.Trim().ToLowerInvariant() switch
        {
            "letter" or "carta" => (215.9f, 279.4f),
            "legal" or "oficio" => (215.9f, 355.6f),
            "a4" => (210f, 297f),
            "a5" => (148f, 210f),
            var other => throw new DocumentValidationException(
                $"Tamaño de página '{other}' no soportado. Usa letter, legal, a4 o a5, o indica width y height.")
        };

        return string.Equals(page.Orientation, "landscape", StringComparison.OrdinalIgnoreCase)
            ? (height, width)
            : (width, height);
    }
}
