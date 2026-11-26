using System.Text.Json;
using ACCloud.DocumentStudio.TemplateEngine.Models;

namespace ACCloud.DocumentStudio.Contracts.Templates;

public class TemplateSummaryResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    /// <summary>Draft | Published | Archived</summary>
    public string Status { get; init; } = string.Empty;

    public int CurrentVersion { get; init; }

    public int? PublishedVersion { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}

/// <summary>Plantilla con la definición de su versión más reciente.</summary>
public sealed class TemplateDetailResponse : TemplateSummaryResponse
{
    public TemplateDefinition Definition { get; init; } = new();

    public JsonElement? SampleData { get; init; }
}

public class TemplateVersionSummaryResponse
{
    public int Version { get; init; }

    public string? Comment { get; init; }

    public DateTime CreatedAt { get; init; }

    public bool IsCurrent { get; init; }

    public bool IsPublished { get; init; }
}

public sealed class TemplateVersionResponse : TemplateVersionSummaryResponse
{
    public Guid TemplateId { get; init; }

    public TemplateDefinition Definition { get; init; } = new();

    public JsonElement? SampleData { get; init; }
}

public sealed class PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int Total { get; init; }
}
