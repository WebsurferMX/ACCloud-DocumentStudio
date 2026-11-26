# Guía de cambios — ACCloud DocumentStudio

Lista los cambios que afectan a quien consume la API o trabaja en el código, con lo que hay que hacer en cada caso.
El detalle técnico y cronológico está en [BITACORA.md](BITACORA.md).

---

## 0.3.0 — 2026-11-25 — Fase 1: motor de secciones y CFDI

### Resumen

- Las plantillas se organizan en **secciones**: `header` (se repite en cada página), `sections[]` (cuerpo) y `footer` (se repite en cada página).
- Una sección con `dataSource` **se repite por cada elemento de un arreglo** (renglones de factura). La paginación es automática.
- Los elementos se dibujan en su **posición real** (`x`, `y`, `width`, `height` en **milímetros**).
- Elementos nuevos: `image`, `line`, `rect`, `qr`, `barcode`.
- Expresiones con contexto `data` / `row` / `sys` y funciones (`ifNull`, `sum`, `formatCurrency`…).
- **CFDI 3.3/4.0:** convertidor XML→JSON, plantilla de factura incluida y endpoints `/api/cfdi/*`.
- Errores de plantilla, datos o XML: **400 ProblemDetails** con mensaje en español (antes, 500 con stack trace).
- `type` ya **no** tiene que ser la primera propiedad de cada elemento.

### ⚠️ Cambios que afectan a clientes existentes

| Antes (0.2) | Ahora (0.3) | Qué hacer |
|---|---|---|
| `x/y/width/height` se ignoraban | Se respetan, en **mm** | Revisar que las medidas estén en mm |
| `fontSize` entero | Decimal, en puntos | Nada (los enteros siguen siendo válidos) |
| Errores devolvían 500 | 400 con `ProblemDetails` | Leer `detail` en lugar del cuerpo en texto |
| `IDocumentRenderer.Render(List<…>)` | `RenderAsync(RenderDocument)` | Solo afecta a código interno |
| `ITemplateCompiler.Compile(t, object)` | `Compile(t, JsonElement)` | Solo afecta a código interno |

`template.elements` (sin sección) **sigue funcionando**: se trata como la primera sección del cuerpo.

### Endpoints

| Método y ruta | Entrada | Salida |
|---|---|---|
| `POST /api/render` | `{ template, data }` | PDF |
| `POST /api/render/preview?page=1&dpi=96` | `{ template, data }` | PNG de la página; header `X-Total-Pages` |
| `POST /api/cfdi/render` | JSON `{ xml, template?, templateName? }` o XML directo (`application/xml`, `?template=`) | PDF, nombrado `{UUID}.pdf` |
| `POST /api/cfdi/data` | JSON `{ xml }` o XML directo | JSON que recibe la plantilla |
| `GET /api/templates/builtin` | — | Nombres de plantillas incluidas |
| `GET /api/templates/builtin/{nombre}` | — | Plantilla en JSON |

Ejemplos listos en [ACCloud.DocumentStudio.Api.http](../src/ACCloud.DocumentStudio.Api/ACCloud.DocumentStudio.Api.http).

### Referencia del contrato de plantilla

```jsonc
{
  "page": { "size": "letter", "orientation": "portrait",      // o width/height en mm
            "margin": { "top": 10, "right": 10, "bottom": 10, "left": 10 } },
  "header":   { "height": 30, "elements": [ ... ] },            // cada página
  "sections": [
    { "name": "conceptos", "height": 9, "dataSource": "data.conceptos",
      "visibleIf": "gt(row.cantidad, 0)", "elements": [ ... ] },
    { "name": "totales", "height": 30, "elements": [ ... ] }
  ],
  "footer":   { "height": 6, "elements": [ ... ] }               // cada página
}
```

- `page.size`: `letter` (por defecto), `legal`, `a4`, `a5`. Sin `size` se usan `width`/`height`.
- `section.height`: si es 0 se calcula con el elemento más bajo. Una sección no se parte entre páginas.

**Propiedades comunes de elementos:** `type`, `id`, `x`, `y`, `width`, `height` (mm, relativos a la sección), `zIndex`, `visible`, `visibleIf`.

| `type` | Propiedades propias |
|---|---|
| `text` | `value`, `fontSize` (pt, 10), `bold`, `italic`, `color`, `backgroundColor`, `align` (left/center/right), `verticalAlign` (top/middle/bottom), `fontFamily`, `wrapAnywhere`. Si no cabe en su caja, se reduce. |
| `image` | `source` (URL http(s), data URI o base64; admite expresiones), `fit` (contain/fill/width/height) |
| `line` | `color`, `thickness` (pt). Horizontal si `width ≥ height` |
| `rect` | `borderColor`, `borderWidth` (pt), `fillColor`, `cornerRadius` (mm) |
| `qr` | `value` |
| `barcode` | `value`, `format` (code128, code39, ean13, ean8, upca, pdf417, datamatrix) |

### Expresiones

Van entre `{{ }}` dentro de cualquier texto, `source` o `value`. En `dataSource` y `visibleIf` se escriben con o sin llaves.

| Raíz | Significado |
|---|---|
| `data.x.y` | JSON del documento. `{{x.y}}` (sin `data.`) también funciona, por compatibilidad |
| `row.x` | Elemento actual en una sección con `dataSource` |
| `rowIndex` / `rowNumber` | Posición del renglón (desde 0 / desde 1) |
| `sys.pageNumber` / `sys.totalPages` | Página actual / total (se resuelven al paginar) |
| `sys.date` / `sys.now` | Fecha de generación |

Índices de arreglo: `data.items[0].nombre`. Literales: `"texto"`, `'texto'`, `12.5`, `true`, `false`, `null`.

| Función | Ejemplo |
|---|---|
| `ifNull(a, b)` | `ifNull(data.serie, "-")` |
| `if(cond, a, b)` | `if(gt(data.descuento, 0), "Con descuento", "")` |
| `eq ne gt gte lt lte` | `eq(data.moneda, "MXN")` |
| `and or not` | `and(data.timbre, not(data.cancelado))` |
| `formatCurrency(x)` | `$1,234.50` (es-MX) |
| `formatNumber(x, fmt)` | `formatNumber(row.cantidad, "N2")` |
| `formatDate(x, fmt)` | `formatDate(data.fecha, "dd/MM/yyyy HH:mm")` |
| `sum(arr, campo)` / `count(arr)` | `sum(data.items, "importe")` |
| `upper lower trim concat left right` | `right(data.timbre.selloCFD, 8)` |
| `importeConLetra(x, moneda)` | `CIEN PESOS 00/100 M.N.` |

Un nombre de función desconocido, un número equivocado de argumentos o un error de sintaxis devuelven 400 indicando la posición del error.

### CFDI

`POST /api/cfdi/data` muestra el JSON exacto que recibe la plantilla. Campos principales:

- `data.serie`, `folio`, `fecha`, `subTotal`, `descuento`, `total`, `moneda`, `tipoCambio`
- `formaPago`/`formaPagoDesc`, `metodoPago`/`metodoPagoDesc`, `tipoDeComprobante`/`tipoDeComprobanteDesc`
- `data.emisor` → `rfc`, `nombre`, `regimenFiscal`, `regimenFiscalDesc`
- `data.receptor` → `rfc`, `nombre`, `domicilioFiscalReceptor`, `regimenFiscalReceptor(Desc)`, `usoCFDI(Desc)`
- `data.conceptos[]` → `claveProdServ`, `cantidad`, `claveUnidad`, `unidad`, `descripcion`, `valorUnitario`, `importe`, `descuento`, `objetoImp`, `traslados[]`, `retenciones[]`
- `data.impuestos` → `totalImpuestosTrasladados`, `totalImpuestosRetenidos`, `ivaTrasladado`, `iepsTrasladado`, `isrRetenido`, `ivaRetenido`, `traslados[]`, `retenciones[]`
- `data.timbre` → `uuid`, `fechaTimbrado`, `rfcProvCertif`, `selloCFD`, `selloSAT`, `noCertificadoSAT`
- **Campos calculados:**
  - `data.qrUrl`: URL de verificación del SAT;
  - `data.cadenaOriginalTimbre`;
  - `data.totalConLetra`.

La plantilla incluida es `cfdi-factura`, en tamaño carta. Ejemplo de salida: [samples/cfdi/factura-ejemplo.pdf](../samples/cfdi/factura-ejemplo.pdf).

El convertidor **no valida** sellos, certificados ni el XSD del SAT; solo lee el XML para imprimirlo.

### Archivos

- **Nuevos:**
  - `TemplateEngine/Expressions/*`: parser, evaluador, funciones, importe con letra;
  - `TemplateEngine/Cfdi/*`;
  - `TemplateEngine/Services/PageSizes.cs`;
  - modelos de sección y de elementos;
  - `Renderer/Services/{HttpImageLoader, BarcodeSvgFactory}.cs`;
  - `Renderer/Models/{RenderDocument, RenderBand, …}`;
  - `Api/Controllers/{CfdiController, TemplatesController}.cs`;
  - `Api/Services/*`;
  - `Api/Templates/cfdi-factura.json`;
  - `samples/cfdi/*`.
- **Eliminados:**
  - `Renderer/Models/RenderPage.cs` y `Renderer/Layout/*`: reemplazados por `RenderDocument`/`RenderBand`;
  - `TemplateEngine/Models/Enums/ElementType.cs`: sin uso.
- **Dependencia nueva:** `ZXing.Net` 0.16.11 (QR y códigos de barras como SVG).

---

## 0.2.0 — 2026-11-25

### ⚠️ Cambio que rompe compatibilidad: los elementos requieren `type`

`POST /api/render` ahora exige que cada objeto de `template.elements` traiga un campo `type`.
Antes, **cualquier** petición con elementos fallaba con 500, así que no hay clientes funcionando que romper. Aun así, el contrato cambió.

**Antes (fallaba siempre):**

```json
{ "value": "Hola {{nombre}}", "fontSize": 14 }
```

**Ahora:**

```json
{ "type": "text", "value": "Hola {{nombre}}", "fontSize": 14 }
```

Reglas:

| Regla | Detalle |
|---|---|
| Valores válidos de `type` | `"text"` (único por ahora) |
| Posición | `type` debe ser la **primera propiedad** del objeto |
| Sin `type` o con valor desconocido | La API responde error (hoy 500, ver pendientes) |

Ejemplo completo:

```http
POST /api/render
Content-Type: application/json

{
  "template": {
    "page": { "width": 600, "height": 800 },
    "elements": [
      { "type": "text", "value": "Hola {{cliente.nombre}}", "fontSize": 14 }
    ]
  },
  "data": { "cliente": { "nombre": "Mundo" } }
}
```

Respuesta: `200 OK`, `application/pdf`, archivo `document.pdf`.

### Cómo agregar un nuevo tipo de elemento

1. Crear la clase en `TemplateEngine/Models/`, heredando de `ElementDefinition`.
2. Registrarla en `ElementDefinition.cs`:
   ```csharp
   [JsonDerivedType(typeof(ImageElementDefinition), "image")]
   ```
3. Manejarla en `TemplateCompiler.Compile` y en `QuestPdfRenderer`.
4. Agregar un caso en `TemplateDefinitionJsonTests`.

### Reorganización de carpetas (sin cambios de namespace)

Los archivos del Renderer se movieron para que la carpeta coincida con el namespace que ya declaraban.
**No cambió ningún `namespace` ni `using`**, así que el código que los consume no necesita ajustes.

| Antes | Ahora |
|---|---|
| `Renderer/Render/Interfaces/IDocumentRenderer.cs` | `Renderer/Interfaces/IDocumentRenderer.cs` |
| `Renderer/Render/Layout/ILayoutEngine.cs` | `Renderer/Layout/ILayoutEngine.cs` |
| `Renderer/Render/Models/SimpleLayoutEngine.cs` | `Renderer/Layout/SimpleLayoutEngine.cs` |
| `Renderer/Render/Services/QuestPdfRenderer.cs` | `Renderer/Services/QuestPdfRenderer.cs` |
| `Api/primer-documento.pdf` | `samples/primer-documento.pdf` |

Archivos eliminados (estaban vacíos o eran plantillas de Visual Studio):

- `Renderer/Render/Interfaces/SimpleLayoutEngine.cs` (vacío, duplicaba el nombre del real)
- `TemplateEngine/Services/TestCompiler.cs` (vacío)
- `Class1.cs` en Application, Contracts, Domain, Persistence, Renderer y TemplateEngine
- `tests/.../UnitTest1.cs` (test vacío)
- Carpeta `ACCloud PDF Builder/` (vacía)

### Solución (`.slnx`)

`ACCloud.DocumentStudio.slnx` solo incluía el proyecto de tests. Ahora incluye los 7 proyectos de `src/`, así que `dotnet build ACCloud.DocumentStudio.slnx` compila todo y Visual Studio los muestra.

### Estructura actual

```
ACCloud DocumentStudio/
├── ACCloud.DocumentStudio.slnx
├── docs/            ← esta guía y la bitácora
├── samples/         ← PDFs de ejemplo
├── src/
│   ├── ACCloud.DocumentStudio.Api/            Controllers, Program.cs
│   ├── ACCloud.DocumentStudio.Application/    (vacío, reservado)
│   ├── ACCloud.DocumentStudio.Contracts/      Render/RenderRequest.cs
│   ├── ACCloud.DocumentStudio.Domain/         Entities, Enums, Interfaces
│   ├── ACCloud.DocumentStudio.Persistence/    (vacío, reservado)
│   ├── ACCloud.DocumentStudio.Renderer/       Interfaces, Layout, Models, Services
│   └── ACCloud.DocumentStudio.TemplateEngine/ Interfaces, Models, Services
└── tests/
    └── ACCloud.DocumentStudio.UnitTests/
```

---

## Pendientes conocidos

Actualizado en 0.3.0. Ya resueltos: posicionamiento real, errores 400, código muerto del layout y `type` en cualquier posición.

| # | Tema | Impacto |
|---|---|---|
| 1 | `TemplateEngine` depende de `Renderer` (el compilador produce `RenderDocument`). | Acopla capas; se atiende en la Fase 2. |
| 2 | `Application` y `Persistence` están vacíos; `ITemplateRepository` no tiene implementación. | Las plantillas solo llegan en el body o como incluidas (Fase 2). |
| 3 | Las imágenes por URL se descargan desde el servidor (http/https, 5 MB, 10 s). | Riesgo de SSRF si la API se expone a terceros: antes de producción, filtrar IPs privadas o usar una lista de dominios permitidos. |
| 4 | Una sección no se parte entre páginas; una sección más alta que la página provoca error. | Diseñar secciones de altura menor a la página. |
| 5 | Si un texto no cabe en su caja se **reduce** de tamaño en lugar de crecer la sección. | Descripciones muy largas se ven pequeñas; "altura automática" queda pendiente. |
| 6 | El QR usa el `Total` tal como viene en el XML y codifica `fe` en la URL. | Verificar contra el validador del SAT con un CFDI real. |
