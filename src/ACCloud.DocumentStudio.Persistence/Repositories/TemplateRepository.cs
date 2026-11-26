using ACCloud.DocumentStudio.Domain.Entities;
using ACCloud.DocumentStudio.Domain.Enums;
using ACCloud.DocumentStudio.Domain.Exceptions;
using ACCloud.DocumentStudio.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ACCloud.DocumentStudio.Persistence.Repositories;

public sealed class TemplateRepository : ITemplateRepository
{
    private readonly DocumentStudioDbContext _context;

    public TemplateRepository(DocumentStudioDbContext context)
    {
        _context = context;
    }

    public Task<Template?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Templates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public Task<TemplateVersion?> GetVersionAsync(
        Guid templateId,
        int versionNumber,
        CancellationToken cancellationToken = default)
    {
        return _context.TemplateVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                v => v.TemplateId == templateId && v.VersionNumber == versionNumber,
                cancellationToken);
    }

    public async Task<IReadOnlyList<TemplateVersion>> ListVersionsAsync(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        return await _context.TemplateVersions
            .AsNoTracking()
            .Where(v => v.TemplateId == templateId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Template> Items, int Total)> ListAsync(
        string? search,
        bool includeArchived,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Templates.AsNoTracking();

        if (!includeArchived)
        {
            query = query.Where(t => t.Status != TemplateStatus.Archived);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{EscapeLike(search.Trim())}%";
            query = query.Where(t =>
                EF.Functions.ILike(t.Name, pattern, "\\")
                || (t.Description != null && EF.Functions.ILike(t.Description, pattern, "\\")));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.UpdatedAt)
            .ThenBy(t => t.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public void Add(Template template)
    {
        _context.Templates.Add(template);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new DomainConflictException(
                "Otra operación modificó la plantilla al mismo tiempo. Vuelve a cargarla e intenta de nuevo.",
                ex);
        }
    }

    private static string EscapeLike(string value)
    {
        return value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
    }
}
