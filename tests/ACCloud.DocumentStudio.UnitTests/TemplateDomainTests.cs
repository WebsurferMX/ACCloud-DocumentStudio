using ACCloud.DocumentStudio.Domain.Entities;
using ACCloud.DocumentStudio.Domain.Enums;
using ACCloud.DocumentStudio.Domain.Exceptions;

namespace ACCloud.DocumentStudio.UnitTests;

public class TemplateDomainTests
{
    private static readonly DateTime Now = new(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);

    private static Template NewTemplate() => Template.Create("Factura", null, "{}", null, Now);

    [Fact]
    public void Create_Starts_As_Draft_With_Version_1()
    {
        var template = NewTemplate();

        Assert.Equal(TemplateStatus.Draft, template.Status);
        Assert.Equal(1, template.CurrentVersion);
        Assert.Null(template.PublishedVersion);
        Assert.Equal(1, Assert.Single(template.Versions).VersionNumber);
    }

    [Fact]
    public void AddVersion_Increments_Number()
    {
        var template = NewTemplate();

        var version = template.AddVersion("{\"a\":1}", null, "cambio", Now.AddMinutes(1));

        Assert.Equal(2, version.VersionNumber);
        Assert.Equal(2, template.CurrentVersion);
        Assert.Equal(Now.AddMinutes(1), template.UpdatedAt);
    }

    [Fact]
    public void ResolveVersion_Prefers_Requested_Then_Published_Then_Latest()
    {
        var template = NewTemplate();
        template.AddVersion("{}", null, null, Now);
        template.AddVersion("{}", null, null, Now);

        Assert.Equal(3, template.ResolveVersion(null));

        template.Publish(2, Now);
        Assert.Equal(TemplateStatus.Published, template.Status);
        Assert.Equal(2, template.ResolveVersion(null));
        Assert.Equal(1, template.ResolveVersion(1));

        template.Unpublish(Now);
        Assert.Equal(3, template.ResolveVersion(null));
        Assert.Throws<DomainValidationException>(() => template.ResolveVersion(9));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Name_Is_Required(string name)
    {
        Assert.Throws<DomainValidationException>(() => Template.Create(name, null, "{}", null, Now));
    }

    [Fact]
    public void Archived_Template_Cannot_Change()
    {
        var template = NewTemplate();
        template.Archive(Now);

        Assert.True(template.IsArchived);
        Assert.Throws<DomainValidationException>(() => template.AddVersion("{}", null, null, Now));
        Assert.Throws<DomainValidationException>(() => template.Publish(1, Now));
        Assert.Throws<DomainValidationException>(() => template.Rename("Otro", null, Now));
    }

    [Fact]
    public void Cannot_Publish_Missing_Version()
    {
        Assert.Throws<DomainValidationException>(() => NewTemplate().Publish(2, Now));
    }
}
