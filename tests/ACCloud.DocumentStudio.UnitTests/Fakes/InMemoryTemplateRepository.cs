using ACCloud.DocumentStudio.Domain.Entities;
using ACCloud.DocumentStudio.Domain.Interfaces;

namespace ACCloud.DocumentStudio.UnitTests.Fakes;

/// <summary>
/// Repositorio en memoria para probar la capa de aplicación sin base de datos.
/// Las versiones viven en la colección de cada plantilla (misma instancia).
/// </summary>
internal sealed class InMemoryTemplateRepository : ITemplateRepository
{
    private readonly Dictionary<Guid, Template> _templates = new();

    public int SaveCount { get; private set; }

    public Task<Template?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_templates.GetValueOrDefault(id));

    public Task<TemplateVersion?> GetVersionAsync(Guid templateId, int versionNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(_templates.GetValueOrDefault(templateId)?.Versions.FirstOrDefault(v => v.VersionNumber == versionNumber));

    public Task<IReadOnlyList<TemplateVersion>> ListVersionsAsync(Guid templateId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TemplateVersion>>(
            _templates.GetValueOrDefault(templateId)?.Versions.OrderByDescending(v => v.VersionNumber).ToList() ?? []);

    public Task<(IReadOnlyList<Template> Items, int Total)> ListAsync(
        string? search,
        bool includeArchived,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _templates.Values
            .Where(t => includeArchived || !t.IsArchived)
            .Where(t => search is null || t.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.UpdatedAt)
            .ToList();

        return Task.FromResult<(IReadOnlyList<Template>, int)>((query.Skip(skip).Take(take).ToList(), query.Count));
    }

    public void Add(Template template) => _templates[template.Id] = template;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
