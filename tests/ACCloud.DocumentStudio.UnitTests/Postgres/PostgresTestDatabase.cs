using ACCloud.DocumentStudio.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ACCloud.DocumentStudio.UnitTests.Postgres;

/// <summary>
/// Base de pruebas <c>documentstudio_test</c>. La cadena sale de la variable
/// DOCUMENTSTUDIO_TEST_CONNECTION o, si no existe, de los user-secrets de la API
/// (cambiando el nombre de la base). Sin cadena, las pruebas se omiten.
/// </summary>
public static class PostgresTestDatabase
{
    public const string ApiUserSecretsId = "3bc64cb7-745b-422f-8d35-68849a746e14";
    public const string DatabaseName = "documentstudio_test";

    public static readonly string? ConnectionString = Resolve();

    private static string? Resolve()
    {
        var explicitConnection = Environment.GetEnvironmentVariable("DOCUMENTSTUDIO_TEST_CONNECTION");

        if (!string.IsNullOrWhiteSpace(explicitConnection))
        {
            return explicitConnection;
        }

        var configured = new ConfigurationBuilder()
            .AddUserSecrets(ApiUserSecretsId)
            .Build()
            .GetConnectionString(DependencyInjection.ConnectionStringName);

        if (string.IsNullOrWhiteSpace(configured))
        {
            return null;
        }

        return new NpgsqlConnectionStringBuilder(configured) { Database = DatabaseName }.ConnectionString;
    }

    public static DocumentStudioDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DocumentStudioDbContext>()
            .UseNpgsql(
                ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", DocumentStudioDbContext.Schema))
            .Options;

        return new DocumentStudioDbContext(options);
    }
}

/// <summary>[Fact] que se omite si no hay PostgreSQL configurado.</summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (PostgresTestDatabase.ConnectionString is null)
        {
            Skip = "PostgreSQL no configurado (DOCUMENTSTUDIO_TEST_CONNECTION o user-secrets de la API).";
        }
    }
}

/// <summary>Recrea la base de pruebas una vez por ejecución y aplica las migraciones.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        if (PostgresTestDatabase.ConnectionString is null)
        {
            return;
        }

        await using var context = PostgresTestDatabase.CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
