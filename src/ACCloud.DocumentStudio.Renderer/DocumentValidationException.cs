namespace ACCloud.DocumentStudio.Renderer;

/// <summary>
/// Error causado por la plantilla o los datos enviados (no por el servidor).
/// La API lo traduce a HTTP 400.
/// </summary>
public sealed class DocumentValidationException : Exception
{
    public DocumentValidationException(string message)
        : base(message)
    {
    }

    public DocumentValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
