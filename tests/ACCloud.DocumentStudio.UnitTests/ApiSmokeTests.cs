using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ACCloud.DocumentStudio.UnitTests;

/// <summary>Levanta la API en memoria (sin base de datos).</summary>
public sealed class ApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiSmokeTests(WebApplicationFactory<Program> factory)
    {
        // Entorno "Testing": no aplica migraciones al arrancar.
        _factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task Swagger_Document_Is_Generated()
    {
        var response = await _factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/api/v1/templates", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Cfdi_Render_Accepts_Raw_Xml()
    {
        var xml = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "TestData", "factura-ejemplo.xml"));

        var response = await _factory.CreateClient().PostAsync(
            "/api/cfdi/render",
            new StringContent(xml, Encoding.UTF8, "application/xml"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Stored_Templates_Return_503_Without_Database()
    {
        // En "Testing" no se cargan user-secrets, así que no hay cadena de conexión.
        var response = await _factory.CreateClient().GetAsync("/api/v1/templates");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("ConnectionStrings:DocumentStudio", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Invalid_Template_Returns_400_Problem()
    {
        var response = await _factory.CreateClient().PostAsync(
            "/api/render",
            new StringContent("""{ "template": { "elements": [ { "value": "sin tipo" } ] } }""", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
