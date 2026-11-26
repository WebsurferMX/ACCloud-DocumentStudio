using System.Text;
using ACCloud.DocumentStudio.Renderer.Interfaces;
using ACCloud.DocumentStudio.Renderer.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ACCloud.DocumentStudio.Renderer.Services;

/// <summary>
/// Dibuja un <see cref="RenderDocument"/> con QuestPDF.
/// Encabezado y pie se repiten en cada página; las bandas del cuerpo
/// se apilan y QuestPDF las pasa a la siguiente página cuando no caben.
/// Dentro de cada banda los elementos van en posición absoluta (capas).
/// </summary>
public sealed class QuestPdfRenderer
    : IDocumentRenderer
{
    private const float MillimetresPerPoint = 25.4f / 72f;

    private readonly IImageLoader _imageLoader;

    public QuestPdfRenderer(IImageLoader imageLoader)
    {
        _imageLoader = imageLoader;
    }

    public async Task<byte[]> RenderAsync(
        RenderDocument document,
        CancellationToken cancellationToken = default)
    {
        var images = await LoadImagesAsync(document, cancellationToken);

        return Build(document, images).GeneratePdf();
    }

    public async Task<IReadOnlyList<byte[]>> RenderPreviewAsync(
        RenderDocument document,
        int dpi = 96,
        CancellationToken cancellationToken = default)
    {
        var images = await LoadImagesAsync(document, cancellationToken);

        var settings = new ImageGenerationSettings
        {
            ImageFormat = ImageFormat.Png,
            RasterDpi = Math.Clamp(dpi, 36, 300)
        };

        return Build(document, images).GenerateImages(settings).ToList();
    }

    private static IDocument Build(
        RenderDocument document,
        IReadOnlyDictionary<string, Image> images)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(new PageSize(document.PageWidth, document.PageHeight, Unit.Millimetre));
                page.MarginTop(document.Margin.Top, Unit.Millimetre);
                page.MarginRight(document.Margin.Right, Unit.Millimetre);
                page.MarginBottom(document.Margin.Bottom, Unit.Millimetre);
                page.MarginLeft(document.Margin.Left, Unit.Millimetre);
                page.DefaultTextStyle(style => style.FontSize(10));

                if (document.Header is { } header)
                {
                    page.Header().Element(c => ComposeBand(c, header, images));
                }

                page.Content().Column(column =>
                {
                    foreach (var band in document.Body)
                    {
                        column.Item().Element(c => ComposeBand(c, band, images));
                    }
                });

                if (document.Footer is { } footer)
                {
                    page.Footer().Element(c => ComposeBand(c, footer, images));
                }
            });
        });
    }

    private static void ComposeBand(
        IContainer container,
        RenderBand band,
        IReadOnlyDictionary<string, Image> images)
    {
        container
            .Height(Math.Max(band.Height, 0.1f), Unit.Millimetre)
            .Layers(layers =>
            {
                layers.PrimaryLayer();

                foreach (var element in band.Elements.OrderBy(e => e.ZIndex))
                {
                    var (width, height) = BoxSize(element);

                    layers.Layer()
                        .OffsetX(element.X, Unit.Millimetre)
                        .OffsetY(element.Y, Unit.Millimetre)
                        .Unconstrained()
                        .Width(width, Unit.Millimetre)
                        .Height(height, Unit.Millimetre)
                        .Element(c => ComposeElement(c, element, images));
                }
            });
    }

    /// <summary>
    /// Las líneas pueden venir con alto (o ancho) 0; se les da al menos
    /// el grosor del trazo para que QuestPDF tenga espacio donde dibujar.
    /// </summary>
    private static (float Width, float Height) BoxSize(RenderElement element)
    {
        if (element is RenderLineElement line)
        {
            var thickness = Math.Max(line.Thickness, 0.1f) * MillimetresPerPoint;

            return line.Width >= line.Height
                ? (Math.Max(line.Width, 0.1f), Math.Max(line.Height, thickness))
                : (Math.Max(line.Width, thickness), Math.Max(line.Height, 0.1f));
        }

        return (Math.Max(element.Width, 0.1f), Math.Max(element.Height, 0.1f));
    }

    private static void ComposeElement(
        IContainer container,
        RenderElement element,
        IReadOnlyDictionary<string, Image> images)
    {
        switch (element)
        {
            case RenderTextElement text:
                ComposeText(container, text);
                break;

            case RenderImageElement image:
                ComposeImage(container, image, images);
                break;

            case RenderLineElement line:
                ComposeLine(container, line);
                break;

            case RenderRectElement rect:
                ComposeRect(container, rect);
                break;

            case RenderBarcodeElement barcode:
                ComposeBarcode(container, barcode);
                break;
        }
    }

    private static void ComposeText(
        IContainer container,
        RenderTextElement element)
    {
        if (!string.IsNullOrEmpty(element.BackgroundColor))
        {
            container = container.Background(element.BackgroundColor);
        }

        container = element.VerticalAlign?.ToLowerInvariant() switch
        {
            "middle" => container.AlignMiddle(),
            "bottom" => container.AlignBottom(),
            _ => container
        };

        // ScaleToFit: si el texto no cabe en su caja se reduce en lugar
        // de provocar un error de layout.
        container.ScaleToFit().Text(text =>
        {
            switch (element.Align?.ToLowerInvariant())
            {
                case "center":
                    text.AlignCenter();
                    break;
                case "right":
                    text.AlignRight();
                    break;
                default:
                    text.AlignLeft();
                    break;
            }

            text.DefaultTextStyle(style =>
            {
                style = style
                    .FontSize(element.FontSize > 0 ? element.FontSize : 10)
                    .FontColor(string.IsNullOrEmpty(element.Color) ? "#000000" : element.Color);

                if (element.Bold)
                {
                    style = style.Bold();
                }

                if (element.Italic)
                {
                    style = style.Italic();
                }

                if (element.WrapAnywhere)
                {
                    style = style.BreakAnywhere();
                }

                if (!string.IsNullOrWhiteSpace(element.FontFamily))
                {
                    style = style.FontFamily(element.FontFamily);
                }

                return style;
            });

            var literal = new StringBuilder();

            foreach (var ch in element.Text)
            {
                if (ch is not (RenderTokens.PageNumber or RenderTokens.TotalPages))
                {
                    literal.Append(ch);
                    continue;
                }

                if (literal.Length > 0)
                {
                    text.Span(literal.ToString());
                    literal.Clear();
                }

                if (ch == RenderTokens.PageNumber)
                {
                    text.CurrentPageNumber();
                }
                else
                {
                    text.TotalPages();
                }
            }

            if (literal.Length > 0)
            {
                text.Span(literal.ToString());
            }
        });
    }

    private static void ComposeImage(
        IContainer container,
        RenderImageElement element,
        IReadOnlyDictionary<string, Image> images)
    {
        if (!images.TryGetValue(element.Source, out var image))
        {
            return;
        }

        var descriptor = container.Image(image);

        switch (element.Fit?.ToLowerInvariant())
        {
            case "fill":
                descriptor.FitUnproportionally();
                break;
            case "width":
                descriptor.FitWidth();
                break;
            case "height":
                descriptor.FitHeight();
                break;
            default:
                descriptor.FitArea();
                break;
        }
    }

    private static void ComposeLine(
        IContainer container,
        RenderLineElement element)
    {
        var color = string.IsNullOrEmpty(element.Color) ? "#000000" : element.Color;
        var thickness = Math.Max(element.Thickness, 0.1f);

        if (element.Width >= element.Height)
        {
            container.AlignTop().LineHorizontal(thickness).LineColor(color);
        }
        else
        {
            container.AlignLeft().LineVertical(thickness).LineColor(color);
        }
    }

    private static void ComposeRect(
        IContainer container,
        RenderRectElement element)
    {
        if (element.CornerRadius > 0)
        {
            container = container.CornerRadius(element.CornerRadius, Unit.Millimetre);
        }

        if (element.BorderWidth > 0)
        {
            container = container
                .Border(element.BorderWidth)
                .BorderColor(string.IsNullOrEmpty(element.BorderColor) ? "#000000" : element.BorderColor);
        }

        if (!string.IsNullOrEmpty(element.FillColor))
        {
            container = container.Background(element.FillColor);
        }
    }

    private static void ComposeBarcode(
        IContainer container,
        RenderBarcodeElement element)
    {
        var svg = BarcodeSvgFactory.Create(element.Value, element.Format);

        if (svg is not null)
        {
            container.Svg(svg).FitArea();
        }
    }

    private async Task<IReadOnlyDictionary<string, Image>> LoadImagesAsync(
        RenderDocument document,
        CancellationToken cancellationToken)
    {
        var sources = AllBands(document)
            .SelectMany(band => band.Elements)
            .OfType<RenderImageElement>()
            .Select(image => image.Source)
            .Where(source => !string.IsNullOrWhiteSpace(source))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var result = new Dictionary<string, Image>(StringComparer.Ordinal);

        foreach (var source in sources)
        {
            var bytes = await _imageLoader.LoadAsync(source, cancellationToken);

            if (bytes is null)
            {
                continue;
            }

            try
            {
                result[source] = Image.FromBinaryData(bytes);
            }
            catch (Exception)
            {
                // Bytes que no son una imagen válida: se omite el elemento.
            }
        }

        return result;
    }

    private static IEnumerable<RenderBand> AllBands(RenderDocument document)
    {
        if (document.Header is not null)
        {
            yield return document.Header;
        }

        foreach (var band in document.Body)
        {
            yield return band;
        }

        if (document.Footer is not null)
        {
            yield return document.Footer;
        }
    }
}
