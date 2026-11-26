using ACCloud.DocumentStudio.TemplateEngine.Services;
using Xunit;

namespace ACCloud.DocumentStudio.UnitTests;

public class TemplateResolverTests
{
    [Fact]
    public void Should_Resolve_Property()
    {
        var resolver = new TemplateResolver();

        var result = resolver.Resolve(
            "Hola {{employee.name}}",
            new
            {
                employee = new
                {
                    name = "Ernesto"
                }
            });

        Assert.Equal(
            "Hola Ernesto",
            result);
    }
}