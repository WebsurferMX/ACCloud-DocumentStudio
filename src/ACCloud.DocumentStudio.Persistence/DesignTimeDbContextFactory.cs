using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ACCloud.DocumentStudio.Persistence;

/// <summary>
/// Solo para <c>dotnet ef migrations add</c>. Generar migraciones no se conecta
/// a la base; para <c>database update</c> usar la variable DOCUMENTSTUDIO_CONNECTION.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DocumentStudioDbContext>
{
    public DocumentStudioDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DOCUMENTSTUDIO_CONNECTION")
                               ?? "Host=localhost;Port=5432;Database=documentstudio;Username=postgres";

        var options = new DbContextOptionsBuilder<DocumentStudioDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", DocumentStudioDbContext.Schema))
            .Options;

        return new DocumentStudioDbContext(options);
    }
}
