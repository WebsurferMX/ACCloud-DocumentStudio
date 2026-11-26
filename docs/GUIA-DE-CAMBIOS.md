# Guía de cambios — ACCloud DocumentStudio

Lista los cambios que afectan a quien consume la API o trabaja en el código, con lo que hay que hacer en cada caso.
El detalle técnico y cronológico está en [BITACORA.md](BITACORA.md).

---

## 0.4.0 — 2026-11-26 — Fase 2: plantillas guardadas en PostgreSQL

### Resumen

- Las plantillas se guardan en **PostgreSQL** con **historial de versiones**: cada guardado crea una versión inmutable.
- Una versión puede **publicarse**. Al generar se usa la versión pedida, si no la publicada, y si no la más reciente. Así se puede editar sin afectar lo que ya está en producción.
- **Rollback** sin perder historial: restaurar una versión anterior crea una versión nueva igual a ella.
- Cada versión guarda **datos de prueba** (`sampleData`) para el editor y la vista previa. Si al generar no se envía `data`, se usan esos.
- Las expresiones se **validan al guardar**: una plantilla con sintaxis inválida no se guarda (400).
- **Eliminación lógica:** archivar oculta la plantilla y bloquea su uso, pero conserva el historial.
- `POST /api/cfdi/render` acepta `templateId` + `version` para usar una plantilla guardada.
- Capas desacopladas: TemplateEngine ya no depende de QuestPDF (proyecto nuevo `Rendering.Abstractions`).

### Endpoints nuevos (v1)

| Método y ruta | Qué hace |
|---|---|
| `POST /api/v1/templates` | Crea la plantilla (versión 1). Body: `{ name, description?, definition, sampleData? }` |
| `POST /api/v1/templates/from-builtin` | La crea copiando una incluida. Body: `{ builtIn: "cfdi-factura", name? }` |
| `GET /api/v1/templates?search=&page=&pageSize=&includeArchived=` | Lista paginada (máx. 100 por página), las más recientes primero |
| `GET /api/v1/templates/{id}` | Detalle con la definición y los datos de prueba de la versión más reciente |
| `PUT /api/v1/templates/{id}` | Renombra y/o crea una versión. Body: `{ name?, description?, definition?, sampleData?, comment? }` |
| `DELETE /api/v1/templates/{id}` | Archiva (204) |
| `POST /api/v1/templates/{id}/publish` | Publica `{ version? }` (por defecto la más reciente) |
| `POST /api/v1/templates/{id}/unpublish` | Quita la publicación |
| `GET /api/v1/templates/{id}/versions` | Historial: `version`, `comment`, `createdAt`, `isCurrent`, `isPublished` |
| `GET /api/v1/templates/{id}/versions/{n}` | Definición y datos de prueba de esa versión |
| `POST /api/v1/templates/{id}/versions/{n}/restore` | Crea una versión nueva igual a la `n` |
| `POST /api/v1/documents` | Genera. Body: `{ templateId, version?, data?, format: "pdf"\|"png", page?, dpi?, fileName? }` |

**Reglas de `PUT`:**

- Solo `name`/`description`: renombra sin crear versión.
- Con `definition` o `sampleData`: crea una versión. Si no se envía `sampleData`, se copian los datos de prueba de la versión actual.

**Códigos de respuesta nuevos:**

| Código | Cuándo |
|---|---|
| 404 | La plantilla o versión no existe; también al generar con una plantilla archivada |
| 409 | Dos guardados simultáneos intentaron crear el mismo número de versión. Volver a cargar e intentar de nuevo |
| 503 | La base no está configurada. Los endpoints sin base (`/api/render`, `/api/cfdi/*` con plantilla incluida, `/api/templates/builtin`) siguen funcionando |

Ejemplos listos en [ACCloud.DocumentStudio.Api.http](../src/ACCloud.DocumentStudio.Api/ACCloud.DocumentStudio.Api.http), sección "API v1".

### Configuración de la base de datos

La API lee `ConnectionStrings:DocumentStudio`. No hay contraseñas en el repositorio.

- **Desarrollo:** user-secrets del proyecto Api.
  ```
  dotnet user-secrets set "ConnectionStrings:DocumentStudio" "Host=localhost;Port=5432;Database=documentstudio;Username=...;Password=..." --project src/ACCloud.DocumentStudio.Api
  ```
- **Servidor:** variable de entorno `ConnectionStrings__DocumentStudio`.
- **Migraciones:** en Development se aplican solas al arrancar (`Database:MigrateOnStartup: true`); crean la base si no existe. En otros entornos están apagadas. Para aplicarlas a mano:
  ```
  set DOCUMENTSTUDIO_CONNECTION=Host=...;Database=documentstudio;Username=...;Password=...
  dotnet ef database update --project src/ACCloud.DocumentStudio.Persistence
  ```
- **Esquema:** `document_studio`, con las tablas `templates`, `template_versions` y `__ef_migrations_history`. Nombres en snake_case; definiciones y datos de prueba en `jsonb`. Índice único `(template_id, version_number)`.
- **Pruebas contra PostgreSQL:** usan la base `documentstudio_test`, que se recrea en cada ejecución. Toman la cadena de `DOCUMENTSTUDIO_TEST_CONNECTION` o, si no existe, de los user-secrets de la API, cambiando el nombre de la base. Sin configuración se marcan como omitidas.

### ⚠️ Cambios internos (código)

| Antes | Ahora |
|---|---|
| `Renderer.Models.*`, `Renderer.DocumentValidationException`, `Renderer.Interfaces.IDocumentRenderer` | Namespaces `Rendering.Abstractions.Models` y `Rendering.Abstractions` (proyecto nuevo) |
| TemplateEngine → Renderer | TemplateEngine → Rendering.Abstractions; solo la API conoce QuestPDF |
| Entidad `Template` con `IsPublished` y `Publish()` | `Status`, `CurrentVersion`, `PublishedVersion`, `AddVersion`, `Publish(n)`, `Archive`, `ResolveVersion` |
| `ITemplateRepository` (3 métodos) | `GetAsync`, `GetVersionAsync`, `ListVersionsAsync`, `ListAsync`, `Add`, `SaveChangesAsync` |
| `Api/Controllers/TemplatesController` (incluidas) | Renombrado `BuiltInTemplatesController`; la ruta `/api/templates/builtin` no cambia |

### Corrección de un error de 0.3.0

- **Swagger:** `/swagger/v1/swagger.json` respondía **500** desde 0.3.0. `/api/cfdi/data` y `/api/cfdi/render` tienen dos acciones con la misma ruta (JSON y XML), y OpenAPI no lo admite.
- **Arreglo:** las variantes XML siguen funcionando, pero ya no aparecen en Swagger; su descripción quedó en las variantes JSON.
- **Prevención:** una prueba nueva levanta la API en memoria y valida Swagger.

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

Actualizado en 0.4.0. Ya resueltos: posicionamiento real, errores 400, código muerto del layout, `type` en cualquier posición, acoplamiento TemplateEngine→Renderer y plantillas guardadas.

| # | Tema | Impacto |
|---|---|---|
| 1 | La API no tiene autenticación. | Cualquiera con acceso de red puede crear, editar o archivar plantillas. API keys y workspaces: Fase 4. |
| 2 | En desarrollo la API se conecta con el superusuario `postgres`. | Para servidores, crear un usuario dedicado con permisos solo sobre la base `documentstudio`. |
| 3 | Las imágenes por URL se descargan desde el servidor (http/https, 5 MB, 10 s). | Riesgo de SSRF si la API se expone a terceros: antes de producción, filtrar IPs privadas o usar una lista de dominios permitidos. |
| 4 | Una sección no se parte entre páginas; una sección más alta que la página provoca error. | Diseñar secciones de altura menor a la página. |
| 5 | Si un texto no cabe en su caja se **reduce** de tamaño en lugar de crecer la sección. | Descripciones muy largas se ven pequeñas; "altura automática" queda pendiente. |
| 6 | El QR usa el `Total` tal como viene en el XML y codifica `fe` en la URL. | Verificar contra el validador del SAT con un CFDI real. |
