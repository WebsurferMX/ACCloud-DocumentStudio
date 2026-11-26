using ACCloud.DocumentStudio.Rendering.Abstractions;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.QrCode.Internal;

namespace ACCloud.DocumentStudio.Renderer.Services;

/// <summary>Genera códigos QR y de barras como SVG (vectorial) con ZXing.Net.</summary>
public static class BarcodeSvgFactory
{
    public static string? Create(
        string value,
        string? format)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var barcodeFormat = ParseFormat(format);

        EncodingOptions options = barcodeFormat == BarcodeFormat.QR_CODE
            ? new QrCodeEncodingOptions
            {
                Width = 256,
                Height = 256,
                Margin = 0,
                ErrorCorrection = ErrorCorrectionLevel.M,
                CharacterSet = "UTF-8"
            }
            : new EncodingOptions
            {
                Width = 400,
                Height = 100,
                Margin = 0,
                PureBarcode = true
            };

        var writer = new BarcodeWriterSvg
        {
            Format = barcodeFormat,
            Options = options
        };

        try
        {
            return writer.Write(value).Content;
        }
        catch (Exception ex) when (ex is ArgumentException or WriterException)
        {
            throw new DocumentValidationException(
                $"El valor '{value}' no es válido para un código '{format ?? "code128"}': {ex.Message}",
                ex);
        }
    }

    private static BarcodeFormat ParseFormat(string? format)
    {
        return (format ?? "code128").Trim().ToLowerInvariant() switch
        {
            "qr" or "qrcode" => BarcodeFormat.QR_CODE,
            "code128" => BarcodeFormat.CODE_128,
            "code39" => BarcodeFormat.CODE_39,
            "ean13" => BarcodeFormat.EAN_13,
            "ean8" => BarcodeFormat.EAN_8,
            "upca" => BarcodeFormat.UPC_A,
            "pdf417" => BarcodeFormat.PDF_417,
            "datamatrix" => BarcodeFormat.DATA_MATRIX,
            var other => throw new DocumentValidationException(
                $"Formato de código '{other}' no soportado. Usa: qr, code128, code39, ean13, ean8, upca, pdf417, datamatrix.")
        };
    }
}
