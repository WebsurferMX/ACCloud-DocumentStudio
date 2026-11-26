namespace ACCloud.DocumentStudio.Domain.Exceptions;

/// <summary>
/// La operación choca con el estado actual (p. ej. dos ediciones simultáneas
/// que crean la misma versión). HTTP 409.
/// </summary>
public sealed class DomainConflictException : Exception
{
    public DomainConflictException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
