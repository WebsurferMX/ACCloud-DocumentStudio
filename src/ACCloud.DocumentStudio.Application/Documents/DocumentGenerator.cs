using System.Text;
using System.Text.Json;
using ACCloud.DocumentStudio.Rendering.Abstractions;
using ACCloud.DocumentStudio.TemplateEngine.Interfaces;
using ACCloud.DocumentStudio.TemplateEngine.Models;

namespace ACCloud.DocumentStudio.Application.Documents;

public sealed class DocumentGenerator : IDocumentGenerator
{
    private readonly ITemplateCompiler _compiler;
    private readonly IDocumentRenderer _renderer;

    public DocumentGenerator(
        ITemplateCompiler compiler,
        IDocumentRenderer renderer)
    {
        _compiler = compiler;
        _renderer = renderer;
    }

    public async Task<RenderedDocument> GenerateAsync(
        TemplateDefinition template,
        JsonElement data,
        string format,
        string fileName,
        int page = 1,
        int dpi = 96,
        CancellationToken cancellationToken = default)
    {
        var document = _compiler.Compile(template, data);
        var safeName = SafeFileName(fileName);

        switch ((format ?? "pdf").Trim().ToLowerInvariant())
        {
            case "pdf":
            {
                var pdf = await _renderer.RenderAsync(document, cancellationToken);

                return new RenderedDocument(pdf, "application/pdf", $"{safeName}.pdf", null);
            }

            case "png":
            {
                var pages = await _renderer.RenderPreviewAsync(document, dpi, cancellationToken);

                if (page < 1 || page > pages.Count)
                {
                    throw new DocumentValidationException(
                        $"La página {page} no existe; el documento tiene {pages.Count} página(s).");
                }

                return new RenderedDocument(pages[page - 1], "image/png", $"{safeName}-{page}.png", pages.Count);
            }

            default:
                throw new DocumentValidationException($"Formato '{format}' no soportado. Usa pdf o png.");
        }
    }

    /// <summary>Nombre de archivo seguro: letras, dígitos, guion y guion bajo.</summary>
    internal static string SafeFileName(string? name)
    {
        var builder = new StringBuilder();

        foreach (var c in (name ?? string.Empty).Normalize(NormalizationForm.FormD))
        {
            if (char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
            {
                builder.Append(c);
            }
            else if (char.IsWhiteSpace(c) || c is '.' or '/')
            {
                builder.Append('-');
            }
        }

        var result = builder.ToString().Trim('-');

        return result.Length == 0 ? "documento" : result[..Math.Min(result.Length, 100)];
    }
}
