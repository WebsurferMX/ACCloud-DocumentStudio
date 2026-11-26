using System.Text.Json;
using ACCloud.DocumentStudio.Rendering.Abstractions;
using ACCloud.DocumentStudio.Rendering.Abstractions.Models;
using ACCloud.DocumentStudio.TemplateEngine.Expressions;
using ACCloud.DocumentStudio.TemplateEngine.Interfaces;
using ACCloud.DocumentStudio.TemplateEngine.Models;

namespace ACCloud.DocumentStudio.TemplateEngine.Services;

public sealed class TemplateCompiler
    : ITemplateCompiler
{
    private readonly ITemplateResolver _resolver;

    public TemplateCompiler(
        ITemplateResolver resolver)
    {
        _resolver = resolver;
    }

    public RenderDocument Compile(
        TemplateDefinition template,
        JsonElement data)
    {
        ArgumentNullException.ThrowIfNull(template);

        var context = new ExpressionContext(data);
        var page = template.Page ?? new PageDefinition();
        var (width, height) = PageSizes.Resolve(page);
        var margin = page.Margin ?? new MarginDefinition();

        var document = new RenderDocument
        {
            PageWidth = width,
            PageHeight = height,
            Margin = new RenderMargin(margin.Top, margin.Right, margin.Bottom, margin.Left)
        };

        if (template.Header is { } header && IsVisible(header.VisibleIf, context))
        {
            document.Header = CompileBand(header, context);
        }

        foreach (var section in BodySections(template))
        {
            if (string.IsNullOrWhiteSpace(section.DataSource))
            {
                if (IsVisible(section.VisibleIf, context))
                {
                    document.Body.Add(CompileBand(section, context));
                }

                continue;
            }

            foreach (var rowContext in Rows(section, context))
            {
                if (IsVisible(section.VisibleIf, rowContext))
                {
                    document.Body.Add(CompileBand(section, rowContext));
                }
            }
        }

        if (template.Footer is { } footer && IsVisible(footer.VisibleIf, context))
        {
            document.Footer = CompileBand(footer, context);
        }

        return document;
    }

    private static IEnumerable<SectionDefinition> BodySections(TemplateDefinition template)
    {
        if (template.Elements is { Count: > 0 } legacy)
        {
            yield return new SectionDefinition { Name = "elements", Elements = legacy };
        }

        foreach (var section in template.Sections ?? [])
        {
            yield return section;
        }
    }

    private IEnumerable<ExpressionContext> Rows(
        SectionDefinition section,
        ExpressionContext context)
    {
        var source = _resolver.Evaluate(section.DataSource!, context);

        if (source is null)
        {
            yield break;
        }

        if (source is not JsonElement { ValueKind: JsonValueKind.Array } rows)
        {
            throw new DocumentValidationException(
                $"El dataSource '{section.DataSource}' de la sección '{section.Name ?? "(sin nombre)"}' no es un arreglo.");
        }

        var index = 0;

        foreach (var row in rows.EnumerateArray())
        {
            yield return context.ForRow(row, index++);
        }
    }

    private RenderBand CompileBand(
        SectionDefinition section,
        ExpressionContext context)
    {
        var elements = section.Elements ?? [];

        var height = section.Height > 0
            ? section.Height
            : elements.Select(e => e.Y + e.Height).DefaultIfEmpty(0).Max();

        var band = new RenderBand { Height = height };

        foreach (var element in elements)
        {
            if (!element.Visible || !IsVisible(element.VisibleIf, context))
            {
                continue;
            }

            band.Elements.Add(CompileElement(element, context));
        }

        return band;
    }

    private RenderElement CompileElement(
        ElementDefinition element,
        ExpressionContext context)
    {
        RenderElement result = element switch
        {
            TextElementDefinition text => new RenderTextElement
            {
                Text = _resolver.Resolve(text.Value, context),
                FontSize = text.FontSize,
                Bold = text.Bold,
                Italic = text.Italic,
                Color = text.Color,
                BackgroundColor = text.BackgroundColor,
                Align = text.Align,
                VerticalAlign = text.VerticalAlign,
                FontFamily = text.FontFamily,
                WrapAnywhere = text.WrapAnywhere
            },
            ImageElementDefinition image => new RenderImageElement
            {
                Source = _resolver.Resolve(image.Source, context),
                Fit = image.Fit
            },
            LineElementDefinition line => new RenderLineElement
            {
                Color = line.Color,
                Thickness = line.Thickness
            },
            RectElementDefinition rect => new RenderRectElement
            {
                BorderColor = rect.BorderColor,
                BorderWidth = rect.BorderWidth,
                FillColor = rect.FillColor,
                CornerRadius = rect.CornerRadius
            },
            QrElementDefinition qr => new RenderBarcodeElement
            {
                Value = _resolver.Resolve(qr.Value, context),
                Format = "qr"
            },
            BarcodeElementDefinition barcode => new RenderBarcodeElement
            {
                Value = _resolver.Resolve(barcode.Value, context),
                Format = barcode.Format
            },
            _ => throw new DocumentValidationException(
                $"Tipo de elemento no soportado: {element.GetType().Name}.")
        };

        result.X = element.X;
        result.Y = element.Y;
        result.Width = element.Width;
        result.Height = element.Height;
        result.ZIndex = element.ZIndex;

        return result;
    }

    private bool IsVisible(
        string? condition,
        ExpressionContext context)
    {
        return string.IsNullOrWhiteSpace(condition)
               || ExpressionValues.IsTruthy(_resolver.Evaluate(condition, context));
    }
}
