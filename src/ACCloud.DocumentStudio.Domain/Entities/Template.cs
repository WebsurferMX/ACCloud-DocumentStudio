namespace ACCloud.DocumentStudio.Domain.Entities;

public sealed class Template
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public bool IsPublished { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public Template(
        string name,
        string description)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }

    public void Publish()
    {
        IsPublished = true;
    }
}