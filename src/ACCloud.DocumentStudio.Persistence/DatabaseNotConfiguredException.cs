namespace ACCloud.DocumentStudio.Persistence;

/// <summary>Falta ConnectionStrings:DocumentStudio. La API responde 503.</summary>
public sealed class DatabaseNotConfiguredException : Exception
{
    public DatabaseNotConfiguredException()
        : base("La base de datos no está configurada: define ConnectionStrings:DocumentStudio "
               + "(user-secrets en desarrollo o la variable de entorno ConnectionStrings__DocumentStudio).")
    {
    }
}
