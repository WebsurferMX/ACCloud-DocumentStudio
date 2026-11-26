using ACCloud.DocumentStudio.Domain.Enums;
using ACCloud.DocumentStudio.Domain.Exceptions;

namespace ACCloud.DocumentStudio.Domain.Entities;

/// <summary>
/// Plantilla con historial de versiones. Al generar se usa la versión pedida,
/// o la publicada, o la más reciente, en ese orden.
/// </summary>
public sealed class Template
{
    public const int MaxNameLength = 200;
    public const int MaxDescriptionLength = 1000;
    public const int MaxCommentLength = 500;

    private readonly List<TemplateVersion> _versions = new();

    private Template()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public TemplateStatus Status { get; private set; }

    /// <summary>Número de la versión más reciente.</summary>
    public int CurrentVersion { get; private set; }

    /// <summary>Versión que se usa por defecto al generar; null = la más reciente.</summary>
    public int? PublishedVersion { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Solo contiene las versiones agregadas en esta unidad de trabajo;
    /// para el historial completo usar el repositorio.
    /// </summary>
    public IReadOnlyCollection<TemplateVersion> Versions => _versions;

    public bool IsArchived => Status == TemplateStatus.Archived;

    public static Template Create(
        string name,
        string? description,
        string definitionJson,
        string? sampleDataJson,
        DateTime now)
    {
        var template = new Template
        {
            Id = Guid.NewGuid(),
            Status = TemplateStatus.Draft,
            CreatedAt = now
        };

        template.Rename(name, description, now);
        template.AddVersion(definitionJson, sampleDataJson, "Versión inicial", now);

        return template;
    }

    public void Rename(string name, string? description, DateTime now)
    {
        EnsureNotArchived();

        name = name?.Trim() ?? string.Empty;

        if (name.Length == 0)
        {
            throw new DomainValidationException("El nombre de la plantilla es obligatorio.");
        }

        if (name.Length > MaxNameLength)
        {
            throw new DomainValidationException($"El nombre no puede tener más de {MaxNameLength} caracteres.");
        }

        if (description?.Length > MaxDescriptionLength)
        {
            throw new DomainValidationException($"La descripción no puede tener más de {MaxDescriptionLength} caracteres.");
        }

        Name = name;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UpdatedAt = now;
    }

    public TemplateVersion AddVersion(
        string definitionJson,
        string? sampleDataJson,
        string? comment,
        DateTime now)
    {
        EnsureNotArchived();

        if (string.IsNullOrWhiteSpace(definitionJson))
        {
            throw new DomainValidationException("La definición de la plantilla es obligatoria.");
        }

        if (comment?.Length > MaxCommentLength)
        {
            throw new DomainValidationException($"El comentario no puede tener más de {MaxCommentLength} caracteres.");
        }

        var version = new TemplateVersion(
            Id,
            CurrentVersion + 1,
            definitionJson,
            sampleDataJson,
            string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            now);

        _versions.Add(version);
        CurrentVersion = version.VersionNumber;
        UpdatedAt = now;

        return version;
    }

    public void Publish(int versionNumber, DateTime now)
    {
        EnsureNotArchived();
        EnsureVersionExists(versionNumber);

        PublishedVersion = versionNumber;
        Status = TemplateStatus.Published;
        UpdatedAt = now;
    }

    public void Unpublish(DateTime now)
    {
        EnsureNotArchived();

        PublishedVersion = null;
        Status = TemplateStatus.Draft;
        UpdatedAt = now;
    }

    public void Archive(DateTime now)
    {
        Status = TemplateStatus.Archived;
        UpdatedAt = now;
    }

    /// <summary>Versión pedida, si no la publicada, si no la más reciente.</summary>
    public int ResolveVersion(int? requested)
    {
        var version = requested ?? PublishedVersion ?? CurrentVersion;

        EnsureVersionExists(version);

        return version;
    }

    private void EnsureVersionExists(int versionNumber)
    {
        if (versionNumber < 1 || versionNumber > CurrentVersion)
        {
            throw new DomainValidationException(
                $"La versión {versionNumber} no existe; la plantilla tiene versiones de 1 a {CurrentVersion}.");
        }
    }

    private void EnsureNotArchived()
    {
        if (IsArchived)
        {
            throw new DomainValidationException("La plantilla está archivada y no se puede modificar.");
        }
    }
}
