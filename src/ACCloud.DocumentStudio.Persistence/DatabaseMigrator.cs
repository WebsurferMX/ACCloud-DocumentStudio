using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ACCloud.DocumentStudio.Persistence;

public static class DatabaseMigrator
{
    /// <summary>
    /// Aplica las migraciones pendientes (crea la base si no existe).
    /// Si la base no está configurada solo registra una advertencia.
    /// </summary>
    public static async Task MigrateAsync(
        IServiceProvider services,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        DocumentStudioDbContext context;

        try
        {
            context = scope.ServiceProvider.GetRequiredService<DocumentStudioDbContext>();
        }
        catch (DatabaseNotConfiguredException ex)
        {
            logger.LogWarning("{Message} Los endpoints de plantillas guardadas responderán 503.", ex.Message);
            return;
        }

        // MigrateAsync crea la base si no existe; consultar migraciones pendientes
        // antes de crearla genera errores de conexión en el log.
        await context.Database.MigrateAsync(cancellationToken);

        var applied = await context.Database.GetAppliedMigrationsAsync(cancellationToken);

        logger.LogInformation(
            "Base de datos al día ({Count} migración(es); última: {Last}).",
            applied.Count(),
            applied.LastOrDefault() ?? "ninguna");
    }
}
