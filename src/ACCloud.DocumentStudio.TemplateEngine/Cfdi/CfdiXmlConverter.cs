using System.Globalization;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;
using ACCloud.DocumentStudio.Rendering.Abstractions;
using ACCloud.DocumentStudio.TemplateEngine.Expressions;

namespace ACCloud.DocumentStudio.TemplateEngine.Cfdi;

/// <summary>
/// Convierte un XML de CFDI (3.3 o 4.0) en el JSON que usan las plantillas
/// (<c>data.emisor.rfc</c>, <c>data.conceptos[]</c>, <c>data.timbre.uuid</c>...).
/// Agrega campos calculados: descripciones de catálogos, <c>qrUrl</c>,
/// <c>cadenaOriginalTimbre</c> y <c>totalConLetra</c>.
/// No valida sellos ni estructura contra el XSD del SAT.
/// </summary>
public static class CfdiXmlConverter
{
    public const string SatVerificationUrl =
        "https://verificacfdi.facturaelectronica.sat.gob.mx/default.aspx";

    public static JsonObject Convert(string xml)
    {
        var root = Parse(xml);

        var emisor = Child(root, "Emisor");
        var receptor = Child(root, "Receptor");
        var timbre = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "TimbreFiscalDigital");

        var data = new JsonObject
        {
            ["version"] = Str(root, "Version"),
            ["serie"] = Str(root, "Serie"),
            ["folio"] = Str(root, "Folio"),
            ["fecha"] = Str(root, "Fecha"),
            ["formaPago"] = Str(root, "FormaPago"),
            ["formaPagoDesc"] = CfdiCatalogs.Describe(CfdiCatalogs.FormaPago, Attr(root, "FormaPago")),
            ["metodoPago"] = Str(root, "MetodoPago"),
            ["metodoPagoDesc"] = CfdiCatalogs.Describe(CfdiCatalogs.MetodoPago, Attr(root, "MetodoPago")),
            ["condicionesDePago"] = Str(root, "CondicionesDePago"),
            ["moneda"] = Str(root, "Moneda"),
            ["tipoCambio"] = Num(root, "TipoCambio"),
            ["subTotal"] = Num(root, "SubTotal"),
            ["descuento"] = Num(root, "Descuento"),
            ["total"] = Num(root, "Total"),
            ["tipoDeComprobante"] = Str(root, "TipoDeComprobante"),
            ["tipoDeComprobanteDesc"] = CfdiCatalogs.Describe(CfdiCatalogs.TipoDeComprobante, Attr(root, "TipoDeComprobante")),
            ["exportacion"] = Str(root, "Exportacion"),
            ["lugarExpedicion"] = Str(root, "LugarExpedicion"),
            ["noCertificado"] = Str(root, "NoCertificado"),
            ["sello"] = Str(root, "Sello"),
            ["emisor"] = new JsonObject
            {
                ["rfc"] = Str(emisor, "Rfc"),
                ["nombre"] = Str(emisor, "Nombre"),
                ["regimenFiscal"] = Str(emisor, "RegimenFiscal"),
                ["regimenFiscalDesc"] = CfdiCatalogs.Describe(CfdiCatalogs.RegimenFiscal, Attr(emisor, "RegimenFiscal"))
            },
            ["receptor"] = new JsonObject
            {
                ["rfc"] = Str(receptor, "Rfc"),
                ["nombre"] = Str(receptor, "Nombre"),
                ["domicilioFiscalReceptor"] = Str(receptor, "DomicilioFiscalReceptor"),
                ["regimenFiscalReceptor"] = Str(receptor, "RegimenFiscalReceptor"),
                ["regimenFiscalReceptorDesc"] = CfdiCatalogs.Describe(CfdiCatalogs.RegimenFiscal, Attr(receptor, "RegimenFiscalReceptor")),
                ["usoCFDI"] = Str(receptor, "UsoCFDI"),
                ["usoCFDIDesc"] = CfdiCatalogs.Describe(CfdiCatalogs.UsoCfdi, Attr(receptor, "UsoCFDI"))
            },
            ["relacionados"] = Relacionados(root),
            ["conceptos"] = Conceptos(root),
            ["impuestos"] = Impuestos(Child(root, "Impuestos")),
            ["timbre"] = Timbre(timbre)
        };

        var total = Attr(root, "Total");

        data["totalConLetra"] = decimal.TryParse(total, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
            ? NumberToWordsEs.Amount(amount, Attr(root, "Moneda"))
            : null;

        data["qrUrl"] = QrUrl(Attr(timbre, "UUID"), Attr(emisor, "Rfc"), Attr(receptor, "Rfc"), total, Attr(timbre, "SelloCFD"));
        data["cadenaOriginalTimbre"] = CadenaOriginalTimbre(timbre);

        return data;
    }

    /// <summary>
    /// URL del QR de la representación impresa (Anexo 20, CFDI 4.0):
    /// id=UUID, re=RFC emisor, rr=RFC receptor, tt=total, fe=últimos 8 caracteres del sello.
    /// </summary>
    public static string? QrUrl(
        string? uuid,
        string? rfcEmisor,
        string? rfcReceptor,
        string? total,
        string? selloCfd)
    {
        if (string.IsNullOrWhiteSpace(uuid))
        {
            return null;
        }

        var fe = selloCfd is { Length: >= 8 } ? selloCfd[^8..] : selloCfd ?? string.Empty;

        return $"{SatVerificationUrl}?id={uuid}"
               + $"&re={Uri.EscapeDataString(rfcEmisor ?? string.Empty)}"
               + $"&rr={Uri.EscapeDataString(rfcReceptor ?? string.Empty)}"
               + $"&tt={total}"
               + $"&fe={Uri.EscapeDataString(fe)}";
    }

    /// <summary>
    /// Cadena original del complemento de certificación digital del SAT:
    /// ||Version|UUID|FechaTimbrado|RfcProvCertif|[Leyenda|]SelloCFD|NoCertificadoSAT||
    /// </summary>
    public static string? CadenaOriginalTimbre(XElement? timbre)
    {
        if (timbre is null)
        {
            return null;
        }

        var parts = new[] { "Version", "UUID", "FechaTimbrado", "RfcProvCertif", "Leyenda", "SelloCFD", "NoCertificadoSAT" }
            .Select(name => Attr(timbre, name))
            .Where(value => !string.IsNullOrEmpty(value));

        return $"||{string.Join('|', parts)}||";
    }

    private static XElement Parse(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            throw new DocumentValidationException("El XML del CFDI está vacío.");
        }

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };

        XDocument document;

        try
        {
            using var reader = XmlReader.Create(new StringReader(xml.TrimStart('﻿')), settings);
            document = XDocument.Load(reader);
        }
        catch (XmlException ex)
        {
            throw new DocumentValidationException($"El XML del CFDI no es válido: {ex.Message}", ex);
        }

        var root = document.Root;

        if (root is null || root.Name.LocalName != "Comprobante")
        {
            throw new DocumentValidationException(
                "El XML no es un CFDI: se esperaba el nodo raíz 'cfdi:Comprobante'.");
        }

        return root;
    }

    private static JsonArray Conceptos(XElement root)
    {
        var result = new JsonArray();

        foreach (var concepto in Children(Child(root, "Conceptos"), "Concepto"))
        {
            var impuestos = Child(concepto, "Impuestos");

            result.Add(new JsonObject
            {
                ["claveProdServ"] = Str(concepto, "ClaveProdServ"),
                ["noIdentificacion"] = Str(concepto, "NoIdentificacion"),
                ["cantidad"] = Num(concepto, "Cantidad"),
                ["claveUnidad"] = Str(concepto, "ClaveUnidad"),
                ["unidad"] = Str(concepto, "Unidad"),
                ["descripcion"] = Str(concepto, "Descripcion"),
                ["valorUnitario"] = Num(concepto, "ValorUnitario"),
                ["importe"] = Num(concepto, "Importe"),
                ["descuento"] = Num(concepto, "Descuento"),
                ["objetoImp"] = Str(concepto, "ObjetoImp"),
                ["objetoImpDesc"] = CfdiCatalogs.Describe(CfdiCatalogs.ObjetoImp, Attr(concepto, "ObjetoImp")),
                ["traslados"] = Taxes(Child(impuestos, "Traslados"), "Traslado"),
                ["retenciones"] = Taxes(Child(impuestos, "Retenciones"), "Retencion")
            });
        }

        return result;
    }

    private static JsonObject Impuestos(XElement? impuestos)
    {
        var traslados = Taxes(Child(impuestos, "Traslados"), "Traslado");
        var retenciones = Taxes(Child(impuestos, "Retenciones"), "Retencion");

        return new JsonObject
        {
            ["totalImpuestosTrasladados"] = Num(impuestos, "TotalImpuestosTrasladados"),
            ["totalImpuestosRetenidos"] = Num(impuestos, "TotalImpuestosRetenidos"),
            ["ivaTrasladado"] = SumByTax(traslados, "002"),
            ["iepsTrasladado"] = SumByTax(traslados, "003"),
            ["isrRetenido"] = SumByTax(retenciones, "001"),
            ["ivaRetenido"] = SumByTax(retenciones, "002"),
            ["traslados"] = traslados,
            ["retenciones"] = retenciones
        };
    }

    private static JsonArray Taxes(XElement? parent, string elementName)
    {
        var result = new JsonArray();

        foreach (var tax in Children(parent, elementName))
        {
            result.Add(new JsonObject
            {
                ["base"] = Num(tax, "Base"),
                ["impuesto"] = Str(tax, "Impuesto"),
                ["impuestoDesc"] = CfdiCatalogs.Describe(CfdiCatalogs.Impuesto, Attr(tax, "Impuesto")),
                ["tipoFactor"] = Str(tax, "TipoFactor"),
                ["tasaOCuota"] = Num(tax, "TasaOCuota"),
                ["importe"] = Num(tax, "Importe")
            });
        }

        return result;
    }

    private static JsonNode? SumByTax(JsonArray taxes, string code)
    {
        var matching = taxes
            .OfType<JsonObject>()
            .Where(t => (string?)t["impuesto"] == code && t["importe"] is not null)
            .ToList();

        return matching.Count == 0
            ? null
            : JsonValue.Create(matching.Sum(t => (decimal)t["importe"]!));
    }

    private static JsonArray Relacionados(XElement root)
    {
        var result = new JsonArray();

        foreach (var group in Children(root, "CfdiRelacionados"))
        {
            var uuids = new JsonArray();

            foreach (var related in Children(group, "CfdiRelacionado"))
            {
                uuids.Add(Attr(related, "UUID"));
            }

            result.Add(new JsonObject
            {
                ["tipoRelacion"] = Str(group, "TipoRelacion"),
                ["uuids"] = uuids
            });
        }

        return result;
    }

    private static JsonObject? Timbre(XElement? timbre)
    {
        if (timbre is null)
        {
            return null;
        }

        return new JsonObject
        {
            ["version"] = Str(timbre, "Version"),
            ["uuid"] = Str(timbre, "UUID"),
            ["fechaTimbrado"] = Str(timbre, "FechaTimbrado"),
            ["rfcProvCertif"] = Str(timbre, "RfcProvCertif"),
            ["leyenda"] = Str(timbre, "Leyenda"),
            ["selloCFD"] = Str(timbre, "SelloCFD"),
            ["noCertificadoSAT"] = Str(timbre, "NoCertificadoSAT"),
            ["selloSAT"] = Str(timbre, "SelloSAT")
        };
    }

    private static XElement? Child(XElement? parent, string localName)
    {
        return parent?.Elements().FirstOrDefault(e => e.Name.LocalName == localName);
    }

    private static IEnumerable<XElement> Children(XElement? parent, string localName)
    {
        return parent?.Elements().Where(e => e.Name.LocalName == localName) ?? [];
    }

    private static string? Attr(XElement? element, string name)
    {
        return element?.Attribute(name)?.Value;
    }

    private static JsonNode? Str(XElement? element, string name)
    {
        return Attr(element, name) is { } value ? JsonValue.Create(value) : null;
    }

    private static JsonNode? Num(XElement? element, string name)
    {
        return decimal.TryParse(Attr(element, name), NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? JsonValue.Create(value)
            : null;
    }
}
