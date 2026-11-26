using System.Text.Json;
using ACCloud.DocumentStudio.Domain.Entities;
using ACCloud.DocumentStudio.Domain.Enums;
using ACCloud.DocumentStudio.Domain.Exceptions;
using ACCloud.DocumentStudio.Persistence.Repositories;
using ACCloud.DocumentStudio.TemplateEngine.Models;
using ACCloud.DocumentStudio.TemplateEngine.Services;

namespace ACCloud.DocumentStudio.UnitTests.Postgres;

/// <summary>Pruebas contra PostgreSQL real (base documentstudio_test).</summary>
public sealed class TemplateRepositoryTests : IClassFixture<PostgresFixture>
{
    private static readonly DateTime Now = new(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);

    private static string Definition(string text) => TemplateJson.Serialize(new TemplateDefinition
    {
        Sections =
        {
            new SectionDefinition
            {
                Height = 10,
                DataSource = "data.items",
                Elements = { new TextElementDefinition { Value = text, Width = 50, Height = 5 } }
            }
        }
    });

    private static async Task<Guid> SeedAsync(string name)
    {
        await using var context = PostgresTestDatabase.CreateContext();
        var repository = new TemplateRepository(context);
        var template = Template.Create(name, "descripción", Definition("{{row.nombre}}"), """{ "items": [1, 2] }""", Now);

        repository.Add(template);
        await repository.SaveChangesAsync();

        return template.Id;
    }

    [PostgresFact]
    public async Task Saves_Template_And_Versions_With_Jsonb()
    {
        var id = await SeedAsync("Repositorio " + Guid.NewGuid());

        await using (var context = PostgresTestDatabase.CreateContext())
        {
            var repository = new TemplateRepository(context);
            var template = (await repository.GetAsync(id))!;

            template.AddVersion(Definition("v2 {{row.nombre}}"), null, "segunda", Now.AddHours(1));
            template.Publish(1, Now.AddHours(1));
            await repository.SaveChangesAsync();
        }

        await using (var context = PostgresTestDatabase.CreateContext())
        {
            var repository = new TemplateRepository(context);
            var template = (await repository.GetAsync(id))!;
            var versions = await repository.ListVersionsAsync(id);
            var first = (await repository.GetVersionAsync(id, 1))!;

            Assert.Equal(2, template.CurrentVersion);
            Assert.Equal(1, template.PublishedVersion);
            Assert.Equal(TemplateStatus.Published, template.Status);
            Assert.Equal([2, 1], versions.Select(v => v.VersionNumber));
            Assert.Equal("segunda", versions[0].Comment);

            // jsonb normaliza el texto, pero la plantilla debe seguir siendo válida.
            var definition = TemplateJson.Deserialize(first.DefinitionJson);
            Assert.Equal("data.items", definition.Sections[0].DataSource);
            Assert.Equal(2, JsonDocument.Parse(first.SampleDataJson!).RootElement.GetProperty("items").GetArrayLength());
        }
    }

    [PostgresFact]
    public async Task Concurrent_Edits_Of_Same_Version_Conflict()
    {
        var id = await SeedAsync("Concurrencia " + Guid.NewGuid());

        await using var first = PostgresTestDatabase.CreateContext();
        await using var second = PostgresTestDatabase.CreateContext();
        var repositoryA = new TemplateRepository(first);
        var repositoryB = new TemplateRepository(second);

        var a = (await repositoryA.GetAsync(id))!;
        var b = (await repositoryB.GetAsync(id))!;

        a.AddVersion(Definition("A"), null, "A", Now);
        b.AddVersion(Definition("B"), null, "B", Now);

        await repositoryA.SaveChangesAsync();

        await Assert.ThrowsAsync<DomainConflictException>(() => repositoryB.SaveChangesAsync());
    }

    [PostgresFact]
    public async Task Lists_With_Search_And_Archived_Filter()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        var active = await SeedAsync($"Lista {tag} activa");
        var archived = await SeedAsync($"Lista {tag} archivada");

        await using (var context = PostgresTestDatabase.CreateContext())
        {
            var repository = new TemplateRepository(context);
            (await repository.GetAsync(archived))!.Archive(Now);
            await repository.SaveChangesAsync();
        }

        await using (var context = PostgresTestDatabase.CreateContext())
        {
            var repository = new TemplateRepository(context);

            var (visible, visibleTotal) = await repository.ListAsync(tag.ToUpperInvariant(), false, 0, 10);
            var (all, allTotal) = await repository.ListAsync(tag, true, 0, 10);
            var (none, _) = await repository.ListAsync("100%_no_existe", true, 0, 10);

            Assert.Equal(1, visibleTotal);
            Assert.Equal(active, Assert.Single(visible).Id);
            Assert.Equal(2, allTotal);
            Assert.Equal(2, all.Count);
            Assert.Empty(none);
        }
    }
}
