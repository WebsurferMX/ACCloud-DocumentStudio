using System.Text.Json;
using System.Text.RegularExpressions;
using ACCloud.DocumentStudio.Renderer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ACCloud.DocumentStudio.Api.Services;

/// <summary>
/// Convierte errores de plantilla, datos o JSON en HTTP 400 (ProblemDetails).
/// Cualquier otra excepción sigue al manejador por defecto (500 sin detalles).
/// </summary>
internal sealed partial class ValidationExceptionHandler
    : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;

    public ValidationExceptionHandler(IProblemDetailsService problemDetails)
    {
        _problemDetails = problemDetails;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var detail = exception switch
        {
            DocumentValidationException validation => validation.Message,
            NotSupportedException notSupported when notSupported.Source == "System.Text.Json"
                => "Cada elemento debe incluir un 'type' válido: text, image, line, rect, qr o barcode."
                   + JsonPath(notSupported.Message),
            JsonException json => $"JSON inválido: {json.Message}",
            _ => null
        };

        if (detail is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Solicitud inválida",
                Detail = detail
            }
        });
    }

    private static string JsonPath(string message)
    {
        var match = PathPattern().Match(message);

        return match.Success ? $" Ruta: {match.Groups[1].Value}" : string.Empty;
    }

    [GeneratedRegex(@"Path: (\S+?)(?: \||\.?$)")]
    private static partial Regex PathPattern();
}
