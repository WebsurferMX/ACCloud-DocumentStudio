using System.Text.Json;
using ACCloud.DocumentStudio.Application.Documents;
using ACCloud.DocumentStudio.Application.Exceptions;
using ACCloud.DocumentStudio.Application.Templates;
using ACCloud.DocumentStudio.Contracts.Documents;
using ACCloud.DocumentStudio.Contracts.Templates;
using ACCloud.DocumentStudio.Renderer.Services;
using ACCloud.DocumentStudio.Rendering.Abstractions;
using ACCloud.DocumentStudio.TemplateEngine.Models;
using ACCloud.DocumentStudio.TemplateEngine.Services;
using ACCloud.DocumentStudio.UnitTests.Fakes;
using QuestPDF.Infrastructure;

namespace ACCloud.DocumentStudio.UnitTests;

public class ApplicationServiceTests
{
    private readonly InMemoryTemplateRepository _repository = new();
    private readonly TemplateCompiler _compiler = new(new TemplateResolver());
    private readonly TemplateService _templates;
    private readonly DocumentService _documents;

    static ApplicationServiceTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public ApplicationServiceTests()
    {
        _templates = new TemplateService(_repository, _compiler, TimeProvider.System);
        _documents = new DocumentService(
            _repository,
            new DocumentGenerator(_compiler, new QuestPdfRenderer(new HttpImageLoader(new HttpClient()))));
    }

    private static TemplateDefinition Greeting(string color = "#000000") => new()
    {
        Sections =
        {
            new SectionDefinition
            {
                Height = 10,
                Elements = { new TextElementDefinition { Value = "Hola {{data.nombre}}", Width = 80, Height = 8, Color = color } }
            }
        }
    };

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;

    private Task<TemplateDetailResponse> CreateAsync() => _templates.CreateAsync(new CreateTemplateRequest
    {
        Name = "Saludo",
        Definition = Greeting(),
        SampleData = Json("""{ "nombre": "Mundo" }""")
    });

    [Fact]
    public async Task Create_Returns_Version_1_With_Sample_Data()
    {
        var created = await CreateAsync();

        Assert.Equal("Draft", created.Status);
        Assert.Equal(1, created.CurrentVersion);
        Assert.Equal("Mundo", created.SampleData!.Value.GetProperty("nombre").GetString());
        Assert.IsType<TextElementDefinition>(Assert.Single(created.Definition.Sections[0].Elements));
    }

    [Fact]
    public async Task Update_With_Definition_Creates_Version_And_Keeps_Sample_Data()
    {
        var created = await CreateAsync();

        var updated = await _templates.UpdateAsync(created.Id, new UpdateTemplateRequest
        {
            Definition = Greeting("#FF0000"),
            Comment = "rojo"
        });

        Assert.Equal(2, updated.CurrentVersion);
        Assert.NotNull(updated.SampleData);

        var versions = await _templates.ListVersionsAsync(created.Id);
        Assert.Equal([2, 1], versions.Select(v => v.Version));
        Assert.Equal("rojo", versions[0].Comment);
    }

    [Fact]
    public async Task Rename_Only_Does_Not_Create_Version()
    {
        var created = await CreateAsync();

        var updated = await _templates.UpdateAsync(created.Id, new UpdateTemplateRequest { Name = "Saludo 2" });

        Assert.Equal("Saludo 2", updated.Name);
        Assert.Equal(1, updated.CurrentVersion);
    }

    [Fact]
    public async Task Restore_Copies_Old_Version_As_New()
    {
        var created = await CreateAsync();
        await _templates.UpdateAsync(created.Id, new UpdateTemplateRequest { Definition = Greeting("#FF0000") });

        var restored = await _templates.RestoreVersionAsync(created.Id, 1);

        Assert.Equal(3, restored.CurrentVersion);
        Assert.Equal("#000000", ((TextElementDefinition)restored.Definition.Sections[0].Elements[0]).Color);
    }

    [Fact]
    public async Task Invalid_Expression_Is_Rejected_On_Save()
    {
        var definition = Greeting();
        ((TextElementDefinition)definition.Sections[0].Elements[0]).Value = "{{ ifNull(data.x }}";

        await Assert.ThrowsAsync<DocumentValidationException>(() => _templates.CreateAsync(new CreateTemplateRequest
        {
            Name = "Mala",
            Definition = definition
        }));
    }

    [Fact]
    public async Task Unknown_Template_Is_Not_Found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _templates.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Document_Uses_Published_Version_And_Sample_Data()
    {
        var created = await CreateAsync();
        await _templates.UpdateAsync(created.Id, new UpdateTemplateRequest { Definition = Greeting("#FF0000") });
        await _templates.PublishAsync(created.Id, 1);

        var stored = await _documents.GetStoredTemplateAsync(created.Id, null);
        Assert.Equal(1, stored.Version);

        var pdf = await _documents.RenderAsync(new CreateDocumentRequest { TemplateId = created.Id });
        Assert.Equal("application/pdf", pdf.ContentType);
        Assert.Equal("Saludo.pdf", pdf.FileName);

        var png = await _documents.RenderAsync(new CreateDocumentRequest { TemplateId = created.Id, Format = "png", Dpi = 36 });
        Assert.Equal(1, png.TotalPages);
    }

    [Fact]
    public async Task Archived_Template_Cannot_Render()
    {
        var created = await CreateAsync();
        await _templates.ArchiveAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _documents.RenderAsync(new CreateDocumentRequest { TemplateId = created.Id }));

        var visible = await _templates.ListAsync(null, includeArchived: false, page: 1, pageSize: 20);
        Assert.Equal(0, visible.Total);
    }

    [Theory]
    [InlineData("Factura Énfasis 2026/11", "Factura-Enfasis-2026-11")]
    [InlineData("../../etc/passwd", "etc-passwd")]
    [InlineData("***", "documento")]
    public void File_Names_Are_Sanitized(string name, string expected)
    {
        Assert.Equal(expected, DocumentGenerator.SafeFileName(name));
    }
}
