namespace ACCloud.DocumentStudio.Domain.Entities;

public sealed class TemplateVersion
{
    public Guid Id { get; private set; }

    public Guid TemplateId { get; private set; }

    public int VersionNumber { get; private set; }

    public string DefinitionJson { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public TemplateVersion(
        Guid templateId,
        int versionNumber,
        string definitionJson)
    {
        Id = Guid.NewGuid();
        TemplateId = templateId;
        VersionNumber = versionNumber;
        DefinitionJson = definitionJson;
        CreatedAt = DateTime.UtcNow;
    }
}