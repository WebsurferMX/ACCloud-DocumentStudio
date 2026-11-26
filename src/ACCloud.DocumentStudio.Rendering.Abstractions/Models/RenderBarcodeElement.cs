namespace ACCloud.DocumentStudio.Rendering.Abstractions.Models;

public sealed class RenderBarcodeElement
    : RenderElement
{
    public string Value { get; set; } = string.Empty;

    /// <summary>qr | code128 | code39 | ean13 | ean8 | upca | pdf417 | datamatrix</summary>
    public string Format { get; set; } = "code128";
}
