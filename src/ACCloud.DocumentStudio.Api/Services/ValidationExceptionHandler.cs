using System.Text.Json;
using System.Text.RegularExpressions;
using ACCloud.DocumentStudio.Application.Exceptions;
using ACCloud.DocumentStudio.Domain.Exceptions;
using ACCloud.DocumentStudio.Persistence;
using ACCloud.DocumentStudio.Rendering.Abstractions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ACCloud.DocumentStudio.Api.Services;

/// <summary>
/// Traduce excepciones conocidas a ProblemDetails:
/// 400 (plantilla, datos, JSON o regla de negocio), 404, 409 y 503.
/// Cualquier otra excepción sigue al manejador por defecto (500 sin detalles).
/// </summary>
internal sealed partial class ValidationExceptionHandler
    : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<ValidationExceptionHandler> _logger;

    public ValidationExceptionHandler(
        IProblemDetailsService problemDetails,
        ILogger<ValidationExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        (int Status, string Title, string Detail)? problem = exception switch
        {
            DocumentValidationException validation
                => (StatusCodes.Status400BadRequest, "Solicitud inválida", validation.Message),
            DomainValidationException domain
                => (StatusCodes.Status400BadRequest, "Solicitud inválida", domain.Message),
            NotSupportedException notSupported when notSupported.Source == "System.Text.Json"
                => (StatusCodes.Status400BadRequest, "Solicitud inválida",
                    "Cada elemento debe incluir un 'type' válido: text, image, line, rect, qr o barcode."
                    + JsonPath(notSupported.Message)),
            JsonException json
                => (StatusCodes.Status400BadRequest, "Solicitud inválida", $"JSON inválido: {json.Message}"),
            NotFoundException notFound
                => (StatusCodes.Status404NotFound, "No encontrado", notFound.Message),
            DomainConflictException conflict
                => (StatusCodes.Status409Conflict, "Conflicto", conflict.Message),
            DatabaseNotConfiguredException database
                => (StatusCodes.Status503ServiceUnavailable, "Base de datos no disponible", database.Message),
            _ => null
        };

        if (problem is not { } value)
        {
            return false;
        }

        if (value.Status >= 500)
        {
            _logger.LogError(exception, "{Title}", value.Title);
        }

        httpContext.Response.StatusCode = value.Status;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = value.Status,
                Title = value.Title,
                Detail = value.Detail
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
