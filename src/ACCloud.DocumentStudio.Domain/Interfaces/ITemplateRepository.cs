using ACCloud.DocumentStudio.Domain.Entities;

namespace ACCloud.DocumentStudio.Domain.Interfaces;

public interface ITemplateRepository
{
    Task<Template?> GetByIdAsync(Guid id);

    Task AddAsync(Template template);

    Task SaveChangesAsync();
}
