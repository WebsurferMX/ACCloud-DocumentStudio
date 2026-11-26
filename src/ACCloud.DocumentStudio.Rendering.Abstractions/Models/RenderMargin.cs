namespace ACCloud.DocumentStudio.Rendering.Abstractions.Models;

/// <summary>Márgenes de página en milímetros.</summary>
public sealed record RenderMargin(
    float Top,
    float Right,
    float Bottom,
    float Left);
