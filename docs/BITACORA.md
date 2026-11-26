# Bitácora del sistema — ACCloud DocumentStudio

Registro cronológico de diagnósticos, cambios y verificaciones. Lo más reciente va arriba.
Para el resumen orientado a consumidores de la API, ver [GUIA-DE-CAMBIOS.md](GUIA-DE-CAMBIOS.md).

Formato de cada entrada: **Contexto → Diagnóstico → Acción → Verificación → Pendiente**.

---

## 2026-11-26 — Fase 2: plantillas guardadas en PostgreSQL

**Contexto.** Fase 2 de la hoja de ruta con PostgreSQL. El usuario eligió usar el servidor local (PostgreSQL 18 como servicio de Windows, puerto 5432) en lugar de un contenedor.

**Diagnóstico previo.**

- En el equipo están PostgreSQL 18.6 corriendo, `dotnet ef` 10.0.15 y Docker Desktop (apagado).
- **Credenciales:** a pedido del usuario se buscaron en el equipo. No hay `pgpass.conf`, y `pg_hba.conf` exige `scram-sha-256`. Se encontró la variable de entorno `CFDI_POSTGRES_PASSWORD`, que funciona con el usuario `postgres`. La contraseña nunca se mostró ni se escribió en el repo.
- En el servidor ya existían las bases `accloud`, `cfdi_mailer`, `cfdi_manager` y `cfdi_sender`. **No se tocaron**; se crearon solo `documentstudio` y `documentstudio_test`.
- **Paquetes:** las últimas versiones de Npgsql EF y EF Design (11.0) son para .NET 11; se fijaron Npgsql.EntityFrameworkCore.PostgreSQL 10.0.4 y Microsoft.EntityFrameworkCore.Design 10.0.15.

**Acción.**

- **`Rendering.Abstractions` (proyecto nuevo):** modelos de render, `IDocumentRenderer` y `DocumentValidationException`. TemplateEngine ya no referencia al Renderer (QuestPDF).
- **Domain:** `Template` con estado, versión actual y versión publicada; `AddVersion`, `Publish`/`Unpublish`, `Archive` y `ResolveVersion`. `TemplateVersion` es inmutable y guarda definición, datos de prueba y comentario. Excepciones `DomainValidationException` (400) y `DomainConflictException` (409). Se borró la carpeta vacía `Value Objects`.
- **Persistence:**
  - `DocumentStudioDbContext`, con esquema `document_studio`, snake_case, `jsonb` e índice único `(template_id, version_number)` contra versiones duplicadas por concurrencia;
  - `TemplateRepository`: búsqueda `ILIKE` con escape; la violación de unicidad se traduce a 409;
  - `DependencyInjection`: la cadena se lee al crear el DbContext; si falta, 503;
  - `DatabaseMigrator`, `DesignTimeDbContextFactory` y la migración `InitialCreate`.
- **Application:**
  - `TemplateService` (CRUD, versiones, publicar, restaurar, validación de expresiones al guardar);
  - `DocumentGenerator`, sin base;
  - `DocumentService`, con plantillas guardadas;
  - `NotFoundException` (404).
- **Contracts:** peticiones y respuestas de plantillas, `PagedResponse`, `CreateDocumentRequest`; `CfdiRenderRequest` con `templateId`/`version`.
- **API:**
  - controladores `V1/TemplatesController` y `V1/DocumentsController`;
  - `CfdiController` acepta plantilla guardada;
  - `BuiltInTemplatesController` (renombrado);
  - manejador de errores con 404/409/503;
  - `UserSecretsId`, cadena de conexión en user-secrets (armada con `CFDI_POSTGRES_PASSWORD`) y `MigrateOnStartup` en Development.

**Errores encontrados y corregidos.**

1. **Swagger respondía 500 desde 0.3.0.** Causa: dos acciones con el mismo método y ruta (variantes JSON y XML de `/api/cfdi/*`). Las variantes XML se ocultaron de Swagger con `ApiExplorerSettings(IgnoreApi = true)` y siguen funcionando.
2. **`/api/cfdi/render` con plantilla incluida respondía 503 sin base.** Lo encontró la prueba nueva `ApiSmokeTests`. `CfdiController` dependía de `DocumentService` → repositorio → DbContext. Se separó `IDocumentGenerator` (sin base) de `IDocumentService`, y el servicio con base solo se resuelve si llega `templateId`.
3. **Errores de conexión en el log** al crear la base desde cero: se consultaban migraciones pendientes antes de que la base existiera. Ahora se llama directo a `MigrateAsync`.

**Verificación.**

- `dotnet build`: 0 errores. `dotnet test`: **82/82** correctas, 0 omitidas. Incluyen 3 pruebas contra PostgreSQL real (jsonb, versiones, conflicto de concurrencia → `DomainConflictException`, búsqueda y archivadas) y 4 pruebas de API en memoria (Swagger, CFDI sin base, 503 sin base, 400).
- API levantada contra `documentstudio`. Flujo completo:
  - crear desde la incluida → v1;
  - listar;
  - nueva versión (nombre del emisor en rojo) → v2, conservando los datos de prueba;
  - publicar la v1;
  - generar sin versión → usa la v1 (mismo tamaño que la incluida);
  - PNG de la v2 → se revisó, nombre en rojo, `X-Total-Pages: 2`;
  - CFDI con `templateId` (XML y JSON) → 200;
  - historial con `isCurrent`/`isPublished` correctos;
  - restaurar la v1 → v3 con el color original;
  - expresión inválida → 400;
  - id inexistente → 404;
  - archivar → 204, y generar después → 404;
  - el listado oculta las archivadas.
- Swagger genera 15 rutas.
- Datos que quedan en `documentstudio`: "Factura ACCloud" (archivada, de la prueba) y "Factura CFDI (desarrollo)" (activa, para seguir probando).

**Pendiente.**

- Sin autenticación: Fase 4.
- En desarrollo se usa el superusuario `postgres`; para servidores, crear un usuario dedicado.

---

## 2026-11-25 — Fase 1: motor de secciones y representación impresa de CFDI

**Contexto.** Arranque de la Fase 1 de [ANALISIS-Y-ROADMAP.md](ANALISIS-Y-ROADMAP.md), con una factura CFDI como caso de prueba.

**Diagnóstico previo.**

- Se revisó la API de QuestPDF 2026.9.2 en su XML de documentación. Hay `Layers`, `OffsetX/OffsetY`, `Unconstrained`, `ScaleToFit`, `BreakAnywhere`, `CurrentPageNumber`/`TotalPages` y `Svg`. No hay `TranslateX` ni una forma de recortar contenido.
- QuestPDF no genera códigos de barras; se agregó ZXing.Net 0.16.11 (salida SVG).

**Acción.**

- **Modelo de plantilla:** `header` / `sections[]` / `footer`; secciones con `dataSource` y `visibleIf`; medidas en mm; tamaños de página predefinidos. `elements` sin sección se conserva por compatibilidad.
- **Expresiones:** parser propio, raíces `data` / `row` / `rowIndex` / `rowNumber` / `sys`, 23 funciones, importe con letra. Cache de expresiones con tope de 10,000.
- **Render:** cada sección es una banda de altura fija con capas en posición absoluta. Header y footer van en cada página. `{{sys.pageNumber}}` y `{{sys.totalPages}}` pasan como marcadores Unicode de uso privado y QuestPDF los resuelve al paginar. El texto que no cabe se escala.
- **Elementos:** text, image (URL/data URI/base64 con límite de 5 MB), line, rect, qr, barcode.
- **CFDI:** `CfdiXmlConverter` (XML 3.3/4.0 a JSON, DTD prohibido, catálogos, `qrUrl`, `cadenaOriginalTimbre`, `totalConLetra`). Plantilla `cfdi-factura.json`. Endpoints `/api/cfdi/render` y `/api/cfdi/data`, con XML en JSON o directo en el body.
- **API:** `POST /api/render/preview` (PNG por página), `GET /api/templates/builtin`, `ValidationExceptionHandler` (400 ProblemDetails) y `AllowOutOfOrderMetadataProperties`.
- **Muestras:** `samples/cfdi/factura-ejemplo.xml` es un CFDI 4.0 ficticio con RFC de prueba del SAT, 32 conceptos y sellos aleatorios no válidos. Se generó con un script fuera del repo. `factura-ejemplo.pdf` es su resultado.
- Eliminados `RenderPage`, `ILayoutEngine`, `SimpleLayoutEngine` y `ElementType` (sin uso).

**Verificación.**

- `dotnet build` de la solución: 0 errores. `dotnet test`: **57/57** correctas (expresiones, importe con letra, compilador, JSON de secciones, CFDI, render de punta a punta con conteo de páginas).
- API levantada:
  - `POST /api/cfdi/render` con el XML de ejemplo → 200, PDF de 2 páginas. Se revisaron las vistas previas: encabezado y pie repetidos, conceptos que pasan a la página 2, "Página X de 2", QR, sellos cortados en cualquier carácter, totales e importe con letra correctos.
  - Errores: sin `type`, expresión con sintaxis inválida, función inexistente y XML que no es CFDI → 400 con mensaje claro, sin stack trace.
  - El contrato 0.2 (elementos sin sección, `type` en otra posición) → 200.
  - El ejemplo del `.http` → 200; vista previa revisada.

**Pendiente.** Ver "Pendientes conocidos" en la guía de cambios: SSRF en imágenes por URL, altura automática de texto, validar el QR contra el verificador del SAT y desacoplar capas (Fase 2).

---

## 2026-11-25 — Análisis de producto de referencia y hoja de ruta

**Contexto.** Se pidió analizar un SaaS de generación de PDF por plantillas (CraftMyPDF) para definir cómo construir algo similar con DocumentStudio.

**Diagnóstico.**

- El dashboard requiere sesión. El navegador del usuario no tenía depuración remota, así que REA no pudo verlo.
- Se abrió Edge con un perfil aislado y depuración remota (`127.0.0.1:9222`). La referencia pública de la API se inspeccionó con REA `inspect_web_page` (evidencia `ev_fa31c1c323c351fa98787f31eb3830bd006c9e55035886b3d96c9d2ffe68828b`). De ahí salen 26 operaciones, los nombres de campos y las regiones.
- Las páginas públicas (features, pricing, seguridad, e-sign) se leyeron para conocer el editor, los planes y la arquitectura declarada.
- Con la sesión del usuario en ese Edge:
  - Bundle del dashboard (`analyze_web_bundle`): app renderizada en el servidor; el editor está en un bundle de 5.7 MB que no se pudo procesar a tiempo.
  - Diseñador con una plantilla de ejemplo (`inspect_web_page` + captura, evidencia `ev_32cc7d78…869a9c9`), solo lectura. Se observaron secciones Header/Normal/Footer con *data-binding*, componentes (Text, Image, Rectangle, Line, QR, Bar-code), expresiones `data.` / `row.` / `sys.` y funciones `ifNull` y `sum`.
  - No se observó el JSON interno de la plantilla.

**Acción.** Se creó [ANALISIS-Y-ROADMAP.md](ANALISIS-Y-ROADMAP.md) con la comparación contra el estado actual y una hoja de ruta en 6 fases. La Fase 6 propone un diferenciador: representación impresa de CFDI 4.0.

**Verificación.** No aplica (documento de análisis; no cambia código).

**Pendiente.** Decidir base de datos, almacenamiento y tecnología del editor; arrancar la Fase 1.

---

## 2026-11-25 — Reorganización de carpetas y limpieza

**Contexto.** La carpeta del Renderer tenía un nivel `Render/` que no coincidía con los namespaces declarados (`Renderer.Interfaces`, `Renderer.Layout`, `Renderer.Services`). `SimpleLayoutEngine` estaba en `Render/Models/`, pero su namespace es `Layout`, y en `Render/Interfaces/` había otro `SimpleLayoutEngine.cs` vacío. La solución `.slnx` solo listaba el proyecto de tests.

**Acción.**

- Respaldo previo del proyecto (sin `bin/` ni `obj/`) en `respaldo-antes-reorganizar.tgz`, en el directorio temporal de la sesión.
- Movidos:
  - `Renderer/Render/Interfaces/IDocumentRenderer.cs` → `Renderer/Interfaces/`
  - `Renderer/Render/Layout/ILayoutEngine.cs` → `Renderer/Layout/`
  - `Renderer/Render/Models/SimpleLayoutEngine.cs` → `Renderer/Layout/`
  - `Renderer/Render/Services/QuestPdfRenderer.cs` → `Renderer/Services/`
  - `Api/primer-documento.pdf` → `samples/`
- Eliminados (vacíos o de plantilla): `Render/Interfaces/SimpleLayoutEngine.cs`, `TemplateEngine/Services/TestCompiler.cs`, los seis `Class1.cs`, `UnitTest1.cs` y la carpeta vacía `ACCloud PDF Builder/`.
- `ACCloud.DocumentStudio.slnx`: agregados los 7 proyectos de `src/` en la carpeta de solución `/src/`.
- Creadas las carpetas `docs/` y `samples/`.
- No se modificó ningún `namespace` ni `using`.

**Verificación.**

- `dotnet build ACCloud.DocumentStudio.slnx` → 0 errores, 0 advertencias.
- `dotnet test` → 3/3 pruebas correctas.
- `POST /api/render` de punta a punta → `200 application/pdf`, 6497 bytes.

**Pendiente.**

- Queda la carpeta vacía `src/ACCloud.DocumentStudio.Renderer/Render/Interfaces/`: Windows la reportó en uso ("Device or resource busy"), probablemente porque el editor la tiene abierta. Se puede borrar a mano después de cerrar VS Code. No afecta la compilación.

---

## 2026-11-25 — `POST /api/render` no generaba el PDF

**Contexto.** Al hacer POST a `/api/render` no se generaba el documento.

**Diagnóstico.**

- Se levantó la API y se envió un POST con un elemento de texto. Respuesta: **500 Internal Server Error**:
  ```
  System.NotSupportedException: Deserialization of interface or abstract types is not supported.
  Type 'ACCloud.DocumentStudio.TemplateEngine.Models.ElementDefinition'. Path: $.template.elements[0]
  ```
- Causa: `TemplateDefinition.Elements` es `IList<ElementDefinition>` y `ElementDefinition` es `abstract`. System.Text.Json no sabe qué clase concreta crear, así que falla al deserializar el body, **antes** de entrar al controller. Ni `TemplateCompiler` ni `QuestPdfRenderer` llegaban a ejecutarse.

**Acción.**

- `TemplateEngine/Models/ElementDefinition.cs`: agregados
  `[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", UnknownDerivedTypeHandling = FailSerialization)]` y
  `[JsonDerivedType(typeof(TextElementDefinition), "text")]`.
- `Api/ACCloud.DocumentStudio.Api.http`: reemplazado el ejemplo `GET /weatherforecast` (de la plantilla, ya no existe) por un `POST /api/render` válido.
- `tests/.../TemplateDefinitionJsonTests.cs` (nuevo):
  - Deserializa un elemento con `"type": "text"` y comprueba que sea `TextElementDefinition` con sus valores.
  - Comprueba que un elemento sin `type` sea rechazado.

**Verificación.**

- Mismo POST con `"type": "text"` → **200 OK**, `application/pdf`, PDF válido (`%PDF-1.4`).
- Pruebas unitarias correctas.

**Pendiente.**

- Errores de JSON devuelven 500 con stack trace; deberían ser 400 (ver pendientes en la guía de cambios).
- El renderer ignora las coordenadas y el tamaño de página.

---

## Estado del sistema al 2026-11-25

| Componente | Estado |
|---|---|
| API (`/api/render`) | Funcional para elementos de texto |
| TemplateEngine (resolver `{{ruta.propiedad}}`) | Funcional, con prueba |
| Renderer (QuestPDF, licencia Community) | Funcional; layout en columna, sin posicionamiento |
| Layout engine | Definido, sin uso |
| Domain (Template, TemplateVersion) | Definido, sin uso |
| Application / Persistence | Vacíos |
| Pruebas unitarias | 3 pruebas, todas correctas |

**Stack:** .NET 10, ASP.NET Core con controllers, Swashbuckle 11.0.1, QuestPDF 2026.9.2, xUnit 2.9.3.
**Puertos locales:** http 5176, https 7184.
