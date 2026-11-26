namespace ACCloud.DocumentStudio.Application.Exceptions;

/// <summary>El recurso no existe (HTTP 404).</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }

    public static NotFoundException Template(Guid id) =>
        new($"La plantilla '{id}' no existe.");

    public static NotFoundException Version(Guid id, int version) =>
        new($"La plantilla '{id}' no tiene la versión {version}.");
}
