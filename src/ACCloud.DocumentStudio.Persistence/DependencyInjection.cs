using ACCloud.DocumentStudio.Domain.Interfaces;
using ACCloud.DocumentStudio.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ACCloud.DocumentStudio.Persistence;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DocumentStudio";

    /// <summary>
    /// Registra EF Core con PostgreSQL. La cadena se lee de
    /// <c>ConnectionStrings:DocumentStudio</c> al crear el DbContext, de modo que
    /// la API arranca aunque falte; los endpoints que usan la base devuelven 503.
    /// </summary>
    public static IServiceCollection AddDocumentStudioPersistence(this IServiceCollection services)
    {
        services.AddDbContext<DocumentStudioDbContext>((provider, options) =>
        {
            var connectionString = provider
                .GetRequiredService<IConfiguration>()
                .GetConnectionString(ConnectionStringName);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new DatabaseNotConfiguredException();
            }

            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", DocumentStudioDbContext.Schema));
        });

        services.AddScoped<ITemplateRepository, TemplateRepository>();

        return services;
    }
}
