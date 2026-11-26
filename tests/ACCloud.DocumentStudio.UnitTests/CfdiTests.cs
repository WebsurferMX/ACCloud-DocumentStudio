using System.Text.Json;
using System.Text.Json.Nodes;
using ACCloud.DocumentStudio.Rendering.Abstractions;
using ACCloud.DocumentStudio.Renderer.Services;
using ACCloud.DocumentStudio.TemplateEngine.Cfdi;
using ACCloud.DocumentStudio.TemplateEngine.Models;
using ACCloud.DocumentStudio.TemplateEngine.Services;
using QuestPDF.Infrastructure;

namespace ACCloud.DocumentStudio.UnitTests;

public class CfdiTests
{
    private static readonly string SampleXml =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "factura-ejemplo.xml"));

    static CfdiTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public void Converts_Cfdi40_To_Template_Data()
    {
        var data = CfdiXmlConverter.Convert(SampleXml);

        Assert.Equal("4.0", (string?)data["version"]);
        Assert.Equal("EKU9003173C9", (string?)data["emisor"]!["rfc"]);
        Assert.Equal("General de Ley Personas Morales", (string?)data["emisor"]!["regimenFiscalDesc"]);
        Assert.Equal("Gastos en general", (string?)data["receptor"]!["usoCFDIDesc"]);
        Assert.Equal("Transferencia electrónica de fondos", (string?)data["formaPagoDesc"]);
        Assert.Equal(146635.83m, (decimal)data["total"]!);
        Assert.Equal(20225.63m, (decimal)data["impuestos"]!["ivaTrasladado"]!);
        Assert.Equal(32, data["conceptos"]!.AsArray().Count);
        Assert.Equal("IVA", (string?)data["conceptos"]![0]!["traslados"]![0]!["impuestoDesc"]);
        Assert.Equal(
            "CIENTO CUARENTA Y SEIS MIL SEISCIENTOS TREINTA Y CINCO PESOS 83/100 M.N.",
            (string?)data["totalConLetra"]);
    }

    [Fact]
    public void Builds_Sat_Qr_Url_And_Cadena_Original()
    {
        var data = CfdiXmlConverter.Convert(SampleXml);
        var timbre = data["timbre"]!;
        var uuid = (string)timbre["uuid"]!;
        var sello = (string)timbre["selloCFD"]!;

        var qr = (string)data["qrUrl"]!;
        Assert.StartsWith(CfdiXmlConverter.SatVerificationUrl + "?id=" + uuid, qr);
        Assert.Contains("&re=EKU9003173C9&rr=URE180429TM6&tt=146635.83&fe=", qr);
        Assert.EndsWith(Uri.EscapeDataString(sello[^8..]), qr);

        Assert.Equal(
            $"||1.1|{uuid}|2026-11-25T10:31:12|SPR190613I52|{sello}|30001000000500003456||",
            (string?)data["cadenaOriginalTimbre"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("no es xml")]
    [InlineData("<factura/>")]
    [InlineData("<!DOCTYPE x [<!ENTITY e SYSTEM \"file:///c:/windows/win.ini\">]><cfdi:Comprobante xmlns:cfdi=\"http://www.sat.gob.mx/cfd/4\">&e;</cfdi:Comprobante>")]
    public void Rejects_Invalid_Xml(string xml)
    {
        Assert.Throws<DocumentValidationException>(() => CfdiXmlConverter.Convert(xml));
    }

    [Fact]
    public async Task Renders_Sample_Invoice_With_Builtin_Template_On_Two_Pages()
    {
        var templateJson = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "cfdi-factura.json"));
        var template = JsonSerializer.Deserialize<TemplateDefinition>(
            templateJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { AllowOutOfOrderMetadataProperties = true })!;

        var data = JsonSerializer.SerializeToElement<JsonNode>(CfdiXmlConverter.Convert(SampleXml));
        var document = new TemplateCompiler(new TemplateResolver()).Compile(template, data);

        Assert.Equal(32 + 2, document.Body.Count); // 32 conceptos + totales + timbre

        var renderer = new QuestPdfRenderer(new HttpImageLoader(new HttpClient()));
        var pdf = await renderer.RenderAsync(document);
        var pages = await renderer.RenderPreviewAsync(document, dpi: 36);

        Assert.Equal("%PDF-"u8.ToArray(), pdf[..5]);
        Assert.Equal(2, pages.Count);
    }
}
