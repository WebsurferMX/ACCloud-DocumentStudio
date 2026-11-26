namespace ACCloud.DocumentStudio.Domain.Exceptions;

/// <summary>Regla de negocio violada por los datos recibidos (HTTP 400).</summary>
public sealed class DomainValidationException : Exception
{
    public DomainValidationException(string message)
        : base(message)
    {
    }
}
